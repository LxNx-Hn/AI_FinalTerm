# Training Step Visuals - BossPPO_FastClear3HitFixed_1000k_0602_v1

Generated from:

- `results/BossPPO_FastClear3HitFixed_1000k_0602_v1/run_logs/Player-0.log`
- `results/BossPPO_FastClear3HitFixed_1000k_0602_v1/run_logs/training_status.json`

## Summary

- Total parsed training episodes: 1943
- Clears / deaths / timeouts: 852 / 1091 / 0
- Last 30 training episodes: 30/30 clears, 0/30 deaths
- Last 30 clear rate: 100.0%
- Final checkpoint step: 1,000,040
- Episode-log x-axis: episode action timeline scaled to final checkpoint step for comparison with TensorBoard

## Figures

![Rolling clear and death rate](01_clear_death_rate_by_step.png)

![Episode reward by step](02_reward_by_step.png)

![Boss damage by step](03_boss_damage_by_step.png)

![Survival and hit rate by step](04_survival_and_hit_rate_by_step.png)

![Checkpoint reward by step](05_checkpoint_reward_by_step.png)

![Clear rate by step band](06_clear_rate_by_step_band.png)

![TensorBoard reward and episode length](07_tensorboard_reward_length.png)

![TensorBoard losses and policy stats](08_tensorboard_losses_policy.png)
