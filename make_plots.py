import json
import matplotlib.pyplot as plt
import os
import numpy as np

def main():
    os.makedirs("results/plots", exist_ok=True)
    
    # Read metrics
    tabular_file = "results/logs/eval_tabular.json"
    dqn_file = "results/logs/eval_dqn.json"
    
    tab = {"boss_kill_rate": 0, "avg_boss_damage_dealt": 0}
    dqn = {"boss_kill_rate": 0, "avg_boss_damage_dealt": 0}
    
    if os.path.exists(tabular_file):
        with open(tabular_file, "r") as f:
            tab = json.load(f)
            
    if os.path.exists(dqn_file):
        with open(dqn_file, "r") as f:
            dqn = json.load(f)

    # Plot Boss Kill Rate
    labels = ['Tabular 30k', 'DQN 10k']
    kill_rates = [tab.get("boss_kill_rate", 0), dqn.get("boss_kill_rate", 0)]
    
    plt.figure(figsize=(8,6))
    plt.bar(labels, kill_rates, color=['blue', 'green'])
    plt.title('Boss Kill Rate (Natural Discovery)')
    plt.ylabel('Kill Rate')
    plt.ylim(0, 1.0)
    for i, v in enumerate(kill_rates):
        plt.text(i, v + 0.02, f"{v:.2f}", ha='center')
    plt.savefig('results/plots/kill_rate_by_method.png')
    plt.close()
    
    # Plot Avg Boss Damage
    damages = [tab.get("avg_boss_damage_dealt", 0), dqn.get("avg_boss_damage_dealt", 0)]
    plt.figure(figsize=(8,6))
    plt.bar(labels, damages, color=['orange', 'red'])
    plt.title('Avg Boss Damage Dealt (Max 60)')
    plt.ylabel('Damage')
    plt.ylim(0, 60)
    for i, v in enumerate(damages):
        plt.text(i, v + 1, f"{v:.1f}", ha='center')
    plt.savefig('results/plots/avg_boss_damage_by_method.png')
    plt.close()
    
    print("Plots generated in results/plots/")

if __name__ == "__main__":
    main()
