import argparse
import json
import os
from boss_env import BossEnv
from state_encoder import encode_state_tabular
from q_agent import QAgent

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--episodes", type=int, default=100)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    env = BossEnv(seed=args.seed)
    agent = QAgent(action_size=10)
    agent.load("results/models/tabular_30k.pkl")
    
    kills = 0
    deaths = 0
    total_boss_dmg = 0
    total_time = 0
    
    for _ in range(args.episodes):
        obs = env.reset()
        state = encode_state_tabular(obs)
        done = False
        while not done:
            action = agent.get_action(state, exploit=True)
            next_obs, _, done, _ = env.step(action)
            state = encode_state_tabular(next_obs)
            
        total_boss_dmg += env.metrics["boss_damage_dealt"]
        if env.metrics["boss_kill"]:
            kills += 1
            total_time += env.step_count * 0.1
        if env.metrics["death"]:
            deaths += 1

    stats = {
        "boss_kill_rate": kills / args.episodes,
        "death_rate": deaths / args.episodes,
        "avg_boss_damage_dealt": total_boss_dmg / args.episodes,
        "avg_clear_time_seconds": (total_time / kills) if kills > 0 else 0
    }
    
    with open("results/logs/eval_tabular.json", "w") as f:
        json.dump(stats, f, indent=2)
        
    print(json.dumps(stats, indent=2))

if __name__ == "__main__":
    main()
