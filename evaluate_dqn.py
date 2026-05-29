import argparse
import json
import os
import csv
import torch
from boss_env import BossEnv
from state_encoder import encode_state_dqn
from dqn_agent import DQNAgent

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--episodes", type=int, default=100)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--device", type=str, default="auto")
    parser.add_argument("--randomize-episode-seed", action="store_true")
    parser.add_argument("--randomize-boss-patterns", action="store_true")
    parser.add_argument("--exploit", action="store_true")
    parser.add_argument("--randomized-patterns", action="store_true", help="Use full randomized boss patterns")
    args = parser.parse_args()

    import config
    config.SEED = args.seed
    config.RANDOMIZE_BOSS_PATTERNS = getattr(args, 'randomize_boss_patterns', False) or getattr(args, 'randomized_patterns', False)
    
    if config.RANDOMIZE_BOSS_PATTERNS:
        args.randomize_episode_seed = True

    if args.device == "auto":
        device = "cuda" if torch.cuda.is_available() else "cpu"
    else:
        device = args.device

    # Dummy env just to get state_dim
    temp_env = BossEnv(seed=args.seed)
    sample_state = encode_state_dqn(temp_env)
    state_dim = len(sample_state)
    
    agent = DQNAgent(state_dim=state_dim, action_size=10, device=device)
    
    model_path = "results/models/dqn_best.pth"
    if not os.path.exists(model_path):
        model_path = "results/models/dqn_last.pth"
    
    import config
    if args.randomize_boss_patterns:
        config.RANDOMIZE_BOSS_PATTERNS = True

    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    if args.device != 'auto':
        device = torch.device(args.device)

    # Dummy env just to get state_dim
    temp_env = BossEnv(seed=args.seed)
    sample_state = encode_state_dqn(temp_env)
    state_dim = len(sample_state)

    model_path = "results/models/dqn_best.pth"
    print(f"Loading best model from {model_path} onto {device}")
    model = DQNAgent(state_dim=state_dim, action_size=10, device=device)
    model.load(model_path)
    if hasattr(model, 'qnetwork_local'):
        model.qnetwork_local.eval()

    best_reward = -float('inf')
    best_trace = []
    best_clear_time = float('inf')

    # To store detailed evaluation metrics
    eval_csv_path = "results/logs/eval_episodes.csv"
    patterns_csv_path = "results/logs/dqn_eval_episode_patterns.csv"
    os.makedirs("results/logs", exist_ok=True)
    
    episode_results = []
    patterns_results = []
    
    kills = 0
    deaths = 0

    print(f"Evaluating for {args.episodes} episodes...")

    with torch.no_grad():
        for ep in range(1, args.episodes + 1):
            ep_seed = args.seed + ep if args.randomize_episode_seed else args.seed
            env = BossEnv(seed=ep_seed)
            obs = env.reset()
            done = False
            total_reward = 0
            action_trace = []
            
            while not done:
                state = encode_state_dqn(env)
                action = model.get_action(state, exploit=True)
                action_trace.append(action)
                
                obs, reward, done, _ = env.step(action)
                total_reward += reward
                
            clear_time = env.step_count * 0.1
            boss_damage = env.metrics["boss_damage_dealt"]
            player_hits = env.metrics.get("hit_while_attacking", 0) + env.metrics.get("hit_while_moving", 0) + env.metrics.get("hit_while_staying", 0)
            death = 1 if env.metrics["death"] else 0
            dash_damage = env.metrics["damage_during_dash_count"]
            no_hit_dash_damage = env.metrics["no_hit_damage_during_dash_count"]
            
            if env.metrics["boss_kill"]:
                kills += 1
            if death:
                deaths += 1
                
            episode_results.append({
                "episode_id": ep,
                "clear_time": clear_time,
                "reward": total_reward,
                "boss_damage": boss_damage,
                "player_hits": player_hits,
                "death": death,
                "damage_during_dash": dash_damage,
                "action_count": len(action_trace)
            })
            
            # Pattern tracking
            seq = []
            if hasattr(env, "director"):
                seq = env.director.executed_sequences
            
            patterns_results.append({
                "episode_id": ep,
                "seed": ep_seed,
                "clear": 1 if env.metrics["boss_kill"] else 0,
                "clear_time_seconds": clear_time,
                "reward": total_reward,
                "boss_damage": boss_damage,
                "player_hits": player_hits,
                "death": death,
                "pattern_sequence": "|".join(seq),
                "pattern_random_choices": "", 
                "no_hit_damage_during_dash_count": no_hit_dash_damage,
                "damage_tile_entries": env.metrics.get("damage_tile_entries", 0)
            })

            # Save best trace based on highest reward (or fastest clear)
            if env.metrics["boss_kill"] and clear_time < best_clear_time:
                best_reward = total_reward
                best_clear_time = clear_time
                best_trace = action_trace
                
    # Write eval_episodes.csv
    with open(eval_csv_path, 'w', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=["episode_id", "clear_time", "reward", "boss_damage", "player_hits", "death", "damage_during_dash", "action_count"])
        writer.writeheader()
        writer.writerows(episode_results)
        
    # Write patterns csv
    with open(patterns_csv_path, 'w', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=[
            "episode_id", "seed", "clear", "clear_time_seconds", "reward", 
            "boss_damage", "player_hits", "death", "pattern_sequence", 
            "pattern_random_choices", "no_hit_damage_during_dash_count", "damage_tile_entries"
        ])
        writer.writeheader()
        writer.writerows(patterns_results)

    # Export best trace to JSON
    trace_path = "results/logs/best_run_trace.json"
    with open(trace_path, "w") as f:
        json.dump({"actions": best_trace}, f, indent=4)
        
    # Compute summary
    boss_kill_rate = kills / args.episodes
    death_rate = deaths / args.episodes
    avg_boss_damage_dealt = sum(r["boss_damage"] for r in episode_results) / args.episodes
    
    cleared_times = [r["clear_time"] for r in episode_results if r["boss_damage"] >= config.BOSS_MAX_HP]
    avg_clear_time = sum(cleared_times) / len(cleared_times) if cleared_times else 0
    std_clear_time = (sum((t - avg_clear_time)**2 for t in cleared_times) / len(cleared_times))**0.5 if cleared_times else 0
    worst_clear_time = max(cleared_times) if cleared_times else 0
    
    avg_reward = sum(r["reward"] for r in episode_results) / args.episodes

    summary = {
        "boss_kill_rate": boss_kill_rate,
        "death_rate": death_rate,
        "avg_boss_damage_dealt": avg_boss_damage_dealt,
        "avg_clear_time_seconds": avg_clear_time,
        "best_clear_time_seconds": best_clear_time if best_clear_time != float('inf') else 0,
        "std_clear_time_seconds": std_clear_time,
        "worst_clear_time_seconds": worst_clear_time,
        "avg_reward": avg_reward
    }

    print(json.dumps(summary, indent=2))

if __name__ == "__main__":
    main()
