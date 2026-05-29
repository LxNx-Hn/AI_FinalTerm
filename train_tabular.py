import argparse
import os
import json
from boss_env import BossEnv
from state_encoder import encode_state_tabular
from q_agent import QAgent

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--episodes", type=int, default=30000)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--curriculum", action="store_true")
    args = parser.parse_args()

    env = BossEnv(seed=args.seed)
    agent = QAgent(action_size=10, epsilon_decay=(0.03**(1.0/(args.episodes*0.8))))

    os.makedirs("results/models", exist_ok=True)
    os.makedirs("results/logs", exist_ok=True)

    for e in range(args.episodes):
        obs = env.reset()
        state = encode_state_tabular(obs)
        done = False
        
        while not done:
            action = agent.get_action(state)
            next_obs, reward, done, _ = env.step(action)
            next_state = encode_state_tabular(next_obs)
            agent.update(state, action, reward, next_state, done)
            state = next_state

        if (e + 1) % 1000 == 0:
            print(f"Tabular Episode {e+1}/{args.episodes}, Epsilon: {agent.epsilon:.4f}, Boss HP: {env.boss_hp}")

    agent.save("results/models/tabular_30k.pkl")

    metrics = {
        "method": "Tabular",
        "episodes": args.episodes,
        "seed": args.seed,
        "curriculum_used": args.curriculum
    }
    with open("results/logs/train_tabular.json", "w") as f:
        json.dump(metrics, f, indent=2)

if __name__ == "__main__":
    main()
