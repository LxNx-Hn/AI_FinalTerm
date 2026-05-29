import argparse
import os
import json
import torch
from boss_env import BossEnv
from state_encoder import encode_state_dqn
from dqn_agent import DQNAgent

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--episodes", type=int, default=10000)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--curriculum", action="store_true")
    parser.add_argument("--device", type=str, default="auto")
    args = parser.parse_args()

    if args.device == "auto":
        device = "cuda" if torch.cuda.is_available() else "cpu"
    else:
        device = args.device

    env = BossEnv(seed=args.seed)
    
    # Run once to get state dim
    sample_state = encode_state_dqn(env)
    state_dim = len(sample_state)
    
    # To reach epsilon_min at 70% of episodes
    decay_steps = args.episodes * 0.7
    epsilon_decay = (0.03)**(1.0/decay_steps)
    
    agent = DQNAgent(state_dim=state_dim, action_size=10, device=device, epsilon_decay=epsilon_decay)

    os.makedirs("results/models", exist_ok=True)
    os.makedirs("results/logs", exist_ok=True)

    best_reward = -float('inf')

    for e in range(args.episodes):
        obs = env.reset()
        state = encode_state_dqn(env)
        done = False
        
        episode_reward = 0
        
        while not done:
            action = agent.get_action(state)
            next_obs, reward, done, _ = env.step(action)
            next_state = encode_state_dqn(env)
            agent.update(state, action, reward, next_state, done)
            state = next_state
            episode_reward += reward
            
        agent.decay_epsilon()

        if episode_reward > best_reward:
            best_reward = episode_reward
            agent.save("results/models/dqn_best.pth")

        if (e + 1) % 1000 == 0:
            print(f"DQN Episode {e+1}/{args.episodes}, Epsilon: {agent.epsilon:.4f}, Boss HP: {env.boss_hp}, Reward: {episode_reward:.1f}")

    agent.save("results/models/dqn_last.pth")

    metrics = {
        "method": "DQN",
        "episodes": args.episodes,
        "seed": args.seed,
        "curriculum_used": args.curriculum
    }
    with open("results/logs/train_dqn.json", "w") as f:
        json.dump(metrics, f, indent=2)

if __name__ == "__main__":
    main()
