import argparse
import json
import os
import torch
from boss_env import BossEnv
from state_encoder import encode_state_dqn
from dqn_agent import DQNAgent

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--episodes", type=int, default=100)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--device", type=str, default="auto")
    args = parser.parse_args()

    if args.device == "auto":
        device = "cuda" if torch.cuda.is_available() else "cpu"
    else:
        device = args.device

    env = BossEnv(seed=args.seed)
    
    sample_state = encode_state_dqn(env)
    state_dim = len(sample_state)
    
    agent = DQNAgent(state_dim=state_dim, action_size=10, device=device)
    
    model_path = "results/models/dqn_best.pth"
    if not os.path.exists(model_path):
        model_path = "results/models/dqn_last.pth"
    
    agent.load(model_path)
    
    kills = 0
    deaths = 0
    total_boss_dmg = 0
    total_time = 0
    best_clear_time = float('inf')
    
    no_hit_dash = 0
    avoidance = 0
    total_reward = 0
    
    best_trace = []
    
    for ep in range(args.episodes):
        obs = env.reset()
        state = encode_state_dqn(env)
        done = False
        
        trace = []
        
        while not done:
            action = agent.get_action(state, exploit=True)
            trace.append(action)
            next_obs, reward, done, _ = env.step(action)
            state = encode_state_dqn(env)
            total_reward += reward
            
        total_boss_dmg += env.metrics["boss_damage_dealt"]
        no_hit_dash += env.metrics["no_hit_damage_during_dash_count"]
        
        if env.metrics["boss_kill"]:
            kills += 1
            clear_time = env.step_count * 0.1
            total_time += clear_time
            if clear_time < best_clear_time:
                best_clear_time = clear_time
                best_trace = trace
        if env.metrics["death"]:
            deaths += 1

    stats = {
        "boss_kill_rate": kills / args.episodes,
        "death_rate": deaths / args.episodes,
        "avg_boss_damage_dealt": total_boss_dmg / args.episodes,
        "avg_clear_time_seconds": (total_time / kills) if kills > 0 else 0,
        "best_clear_time_seconds": best_clear_time if kills > 0 else 0,
        "avg_reward": total_reward / args.episodes
    }
    
    with open("results/logs/eval_dqn.json", "w") as f:
        json.dump(stats, f, indent=2)
        
    with open("results/logs/dqn_best_action_trace.json", "w") as f:
        json.dump({"actions": best_trace}, f)
        
    print(json.dumps(stats, indent=2))

if __name__ == "__main__":
    main()
