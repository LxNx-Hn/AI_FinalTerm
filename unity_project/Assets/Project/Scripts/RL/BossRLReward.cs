using UnityEngine;

[DisallowMultipleComponent]
public class BossRLReward : MonoBehaviour
{
    // ── Reward constants ──────────────────────────────────────────────────────
    // Attack reward is ONLY for actual boss HP reduction — no reward for pressing attack.
    // missed_attack (-0.05): attack-ready + boss out of range → penalizes random swinging.
    // attack_on_cooldown (-0.01): spamming attack while reloading → small deterrent.
    // approach (+0.01/cell): gradient toward boss before first hit is discovered.
    public const float BossDamagePerHp               =  0.10f;
    public const float BossKillReward                =  5.0f;
    public const float MissedAttackPenalty           = -0.08f;  // was -0.05; ready + out-of-range attack (84.9% of attacks out of range)
    public const float AttackOnCooldownPenalty       = -0.02f;
    public const float PlayerHitPenalty              = -2.0f;
    public const float PlayerDeathPenalty            = -8.0f;
    public const float StepPenalty                   = -0.001f;
    public const float WarningTilePenalty            = -0.10f;  // was -0.05; 95.5% of hits on warning tile
    public const float DamageTilePenalty             = -0.20f;
    public const float WallBlockedMovePenalty        = -0.05f;
    public const float ApproachRewardScale           =  0f;
    // New: penalize moving INTO hazard tiles; priority: damage > recent_warn > warn
    public const float MovedIntoWarningPenalty       = -0.08f;
    public const float MovedIntoRecentWarningPenalty = -0.10f;
    public const float MovedIntoDamagePenalty        = -0.30f;
    // New: small reward for attacking while safe (attack_ready + in_range + no danger)
    public const float SafeInRangeAttackAttemptReward =  0.08f;
    public const float MissedSafeAttackOpportunityPenalty = -0.003f;

    public static float TerminalRewardForReason(string reason)
    {
        if (reason == "player_dead") return PlayerDeathPenalty;
        if (reason == "boss_dead")   return BossKillReward;
        return 0f;
    }

    public struct StepResult
    {
        public float reward;
        // per-component breakdown
        public float rewardBossDamage;
        public float rewardHitPenalty;
        public float rewardDeathPenalty;
        public float rewardWarningTile;
        public float rewardDamageTile;
        public float rewardWallBlocked;
        public float rewardApproach;
        public float rewardMissedAttack;
        public float rewardAttackOnCooldown;
        public float rewardMovedIntoDanger; // combined move-into-danger penalty
        public float rewardSafeAttack;      // safe in-range attack attempt reward
        public float rewardMissedSafeAttackOpportunity;
        // episode control
        public bool bossDead;
        public bool playerDead;
        public bool timedOut;
        // raw deltas / state for metrics
        public int  bossDamageDelta;
        public int  playerHitDelta;
        public bool onWarningTile;
        public bool onDamageTile;
        public bool wallBlockedMove;
        public bool missedAttack;       // ready + out-of-range attack fired
        public bool attackOnCooldown;   // attack input while cooldown active
    }

    private bool initialized;
    private int  lastBossHp;
    private int  lastPlayerHp;
    private int  lastManhattanDist = -1;  // -1 = not yet initialized

    public void ResetState(BossRLStateExtractor extractor)
    {
        initialized       = false;
        lastManhattanDist = -1;
        Prime(extractor);
    }

    /// <summary>
    /// Evaluate one decision step.
    /// attackAction     : 0=no attack, 1=attack pressed this step
    /// attackWasReady   : was IsAttackReady true BEFORE ApplyAction?
    /// bossInRange      : is boss inside the 6-cell attack area?
    /// </summary>
    public StepResult Evaluate(
        BossRLStateExtractor extractor,
        float elapsedSeconds,
        float maxEpisodeSeconds,
        bool  isWallBlocked          = false,
        int   attackAction           = 0,
        bool  attackWasReady         = false,
        bool  bossInRange            = false,
        bool  movedIntoWarning       = false,
        bool  movedIntoRecentWarning = false,
        bool  movedIntoDamage        = false,
        bool  safeAttackAttempt      = false,
        bool  missedSafeAttackOpportunity = false)
    {
        Prime(extractor);

        if (extractor == null || !extractor.IsReady)
            return new StepResult();

        int bossHp   = extractor.BossCurrentHp;
        int playerHp = extractor.PlayerCurrentHp;

        float bossDamageR      = 0f;
        float hitPenaltyR      = 0f;
        float deathPenaltyR    = 0f;
        float warningTileR     = 0f;
        float damageTileR      = 0f;
        float wallBlockedR     = 0f;
        float approachR        = 0f;
        float missedAttackR    = 0f;
        float cooldownAttackR  = 0f;
        float movedIntoDangerR = 0f;
        float safeAttackR      = 0f;
        float missedSafeOpportunityR = 0f;

        int bossDelta   = Mathf.Max(0, lastBossHp   - bossHp);
        int playerDelta = Mathf.Max(0, lastPlayerHp - playerHp);

        // Boss damage: +0.05 per HP dealt (direct, not relative to maxHp)
        if (bossDelta > 0)
            bossDamageR = bossDelta * BossDamagePerHp;

        if (playerDelta > 0)
            hitPenaltyR = PlayerHitPenalty * playerDelta;

        extractor.GetHazardState(out bool onWarning, out bool onDamage);
        if (onWarning) warningTileR = WarningTilePenalty;
        if (onDamage)  damageTileR  = DamageTilePenalty;

        if (isWallBlocked) wallBlockedR = WallBlockedMovePenalty;

        // Approach reward: positive when closing distance, negative when increasing distance.
        int curDist = extractor.ManhattanDistanceToBoss();
        if (lastManhattanDist >= 0)
            approachR = (lastManhattanDist - curDist) * ApproachRewardScale;
        lastManhattanDist = curDist;

        // ── Attack quality penalty ───────────────────────────────────────────
        // No reward for pressing attack itself — only boss HP delta gives reward.
        // Penalize clearly wasteful attack inputs:
        //   cooldown hit    : attack spammed while reload timer running   → -0.01
        //   out-of-range hit: attack ready but boss outside 6-cell range  → -0.05
        bool missedAttack    = false;
        bool attackOnCooldown = false;

        if (attackAction == 1)
        {
            if (!attackWasReady)
            {
                attackOnCooldown  = true;
                cooldownAttackR   = AttackOnCooldownPenalty;
            }
            else if (!bossInRange)
            {
                missedAttack   = true;
                missedAttackR  = MissedAttackPenalty;
            }
            // attackWasReady && bossInRange → potential real hit; no extra penalty
        }

        // Move-into-danger penalties: apply highest-severity one only
        if (movedIntoDamage)
            movedIntoDangerR = MovedIntoDamagePenalty;
        else if (movedIntoRecentWarning)
            movedIntoDangerR = MovedIntoRecentWarningPenalty;
        else if (movedIntoWarning)
            movedIntoDangerR = MovedIntoWarningPenalty;

        // Safe in-range attack attempt: small reward for attacking in safe conditions
        if (safeAttackAttempt)
            safeAttackR = SafeInRangeAttackAttemptReward;

        // Missed-safe-opportunity penalty is applied by BossPlayerAgent after a
        // 1s no-hit grace window, so valid evasive moves are not punished here.

        bool bossDead   = extractor.BossIsDead;
        bool playerDead = extractor.PlayerIsDead;
        bool timedOut   = maxEpisodeSeconds > 0f && elapsedSeconds >= maxEpisodeSeconds;

        if (bossDead)   bossDamageR  += BossKillReward;
        if (playerDead) deathPenaltyR = PlayerDeathPenalty;

        float total = StepPenalty + bossDamageR + hitPenaltyR + deathPenaltyR
                      + warningTileR + damageTileR + wallBlockedR
                      + approachR + missedAttackR + cooldownAttackR
                      + movedIntoDangerR + safeAttackR + missedSafeOpportunityR;

        lastBossHp   = bossHp;
        lastPlayerHp = playerHp;

        return new StepResult
        {
            reward                = total,
            rewardBossDamage      = bossDamageR,
            rewardHitPenalty      = hitPenaltyR,
            rewardDeathPenalty    = deathPenaltyR,
            rewardWarningTile     = warningTileR,
            rewardDamageTile      = damageTileR,
            rewardWallBlocked     = wallBlockedR,
            rewardApproach        = approachR,
            rewardMissedAttack    = missedAttackR,
            rewardAttackOnCooldown = cooldownAttackR,
            rewardMovedIntoDanger = movedIntoDangerR,
            rewardSafeAttack      = safeAttackR,
            rewardMissedSafeAttackOpportunity = missedSafeOpportunityR,
            bossDead              = bossDead,
            playerDead            = playerDead,
            timedOut              = timedOut,
            bossDamageDelta       = bossDelta,
            playerHitDelta        = playerDelta,
            onWarningTile         = onWarning,
            onDamageTile          = onDamage,
            wallBlockedMove       = isWallBlocked,
            missedAttack          = missedAttack,
            attackOnCooldown      = attackOnCooldown,
        };
    }

    private void Prime(BossRLStateExtractor extractor)
    {
        if (initialized || extractor == null || !extractor.IsReady) return;
        lastBossHp   = extractor.BossCurrentHp;
        lastPlayerHp = extractor.PlayerCurrentHp;
        initialized  = true;
    }
}
