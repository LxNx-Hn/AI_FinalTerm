import random
import config
from boss_patterns import PATTERN_IDS, normal_scratch_directional, pattern_dash_start, pattern_dash_end, directional_edge_cell
import boss_patterns

class BossDirector:
    def __init__(self, env):
        self.env = env
        self.rng = env.rng  # Use the env's RNG for determinism when seeded
        
        self.current_phase = 1
        self.phase1_first_done = False
        self.phase2_first_done = False
        self.phase3_first_done = False
        self.final_triggered = False
        self.phase2_triggered = False
        self.phase3_triggered = False
        
        # We will accumulate executed pattern tokens here for logging
        self.executed_sequences = []

    def log_pattern(self, name: str):
        self.executed_sequences.append(name)
        
    def _add_centered_dash(self, cells, dash_dir, warning_sec, damage_sec, pattern_id, hide_after=True):
        start = boss_patterns.get_centered_dash_start(cells, dash_dir)
        end = boss_patterns.get_centered_dash_end(start, dash_dir)
        self.env._add_dash_cast(cells, warning_sec, damage_sec, pattern_id, start, end, hide_after)

    def evaluate_phase(self):
        hp = self.env.boss_hp
        max_hp = config.BOSS_MAX_HP
        
        hp_percent = (hp / max_hp) * 100
        
        if not self.final_triggered and hp_percent <= 10:
            self.final_triggered = True
            self.phase3_triggered = True
            self.phase2_triggered = True
            self.current_phase = 4 # Final
            return
            
        if not self.phase3_triggered and hp_percent <= 40:
            self.phase3_triggered = True
            self.phase2_triggered = True
            self.current_phase = 3
            return
            
        if not self.phase2_triggered and hp_percent <= 70:
            self.phase2_triggered = True
            self.current_phase = 2
            return

    def expand_token(self, token: str):
        if token == "basic_rep":
            self.env.pattern_tokens.insert(0, "basic_rep_step")
            
        elif token == "boss_loop":
            self.evaluate_phase()
            if self.current_phase == 4:
                self.env.pattern_tokens = ["pattern_entry_dash", "final_phase_routine", "boss_loop"]
            else:
                self.env.pattern_tokens = ["basic_attack_loop", "evaluate_and_entry", "boss_loop"]
                
        elif token == "evaluate_and_entry":
            self.evaluate_phase()
            self.env.pattern_tokens.insert(0, "run_phase_cycle")
            self.env.pattern_tokens.insert(0, "pattern_entry_dash")
            
        elif token == "pattern_entry_dash":
            self.log_pattern("PatternEntryDash")
            cells = boss_patterns.forward_stripe3_from_cell(self.env.boss_logic_cell, self.env.boss_facing)
            self._add_centered_dash(cells, self.env.boss_facing, config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "run_phase_cycle":
            if self.current_phase == 4:
                self.env.pattern_tokens.insert(0, "final_phase_routine")
            else:
                if self.current_phase == 1:
                    if not self.phase1_first_done:
                        self.phase1_first_done = True
                        self.env.pattern_tokens.insert(0, "phase1_first_fixed")
                    else:
                        self.env.pattern_tokens.insert(0, "run_two_additional")
                elif self.current_phase == 2:
                    if not self.phase2_first_done:
                        self.phase2_first_done = True
                        self.env.pattern_tokens.insert(0, "phase2_first_fixed")
                    else:
                        self.env.pattern_tokens.insert(0, "run_two_additional")
                elif self.current_phase == 3:
                    if not self.phase3_first_done:
                        self.phase3_first_done = True
                        self.env.pattern_tokens.insert(0, "phase3_first_fixed")
                    else:
                        self.env.pattern_tokens.insert(0, "run_two_additional")
            # After run_phase_cycle, boss does LandingSlam
            self.env.pattern_tokens.insert(1, "landing_slam")
            
        elif token == "phase1_first_fixed":
            self.log_pattern("Phase1FirstFixed")
            self.env._add_dash_cast(boss_patterns.diagonal_tr_bl_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (3, 3), (-3, -3), hide_after=True)
            self.env.events.append(self.env._create_gap_event(0.5))
            self.env._add_dash_cast(boss_patterns.diagonal_tl_br_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (-3, 3), (3, -3), hide_after=True)
            self.env.events.append(self.env._create_gap_event(0.5))
            
            # AdditionalPattern1(allowC=False) means 0 or 1
            choice = self.rng.choice([0, 1])
            self.env.pattern_tokens.insert(0, f"run_additional_{choice}")
            
        elif token == "phase2_first_fixed":
            self.log_pattern("Phase2FirstFixed")
            self.env.pattern_tokens.insert(0, "additional_phase2_plus")
            self.env.pattern_tokens.insert(0, "gap")
            self.env.pattern_tokens.insert(0, "phase2_sweep_only")
            
        elif token == "phase3_first_fixed":
            self.log_pattern("Phase3FirstFixed")
            follow_up = 4 if self.rng.random() < 0.5 else 6
            self.env.pattern_tokens.insert(0, f"run_additional_{follow_up}")
            self.env.pattern_tokens.insert(0, "gap")
            self.env.pattern_tokens.insert(0, "mark_dash_4")
            
        elif token == "run_two_additional":
            if self.current_phase == 1:
                pool = [0, 1, 2, 3]
            elif self.current_phase == 3:
                pool = [6, 4, 5]
            else:
                pool = [6, 4]
                
            first = self.rng.choice(pool)
            pool.remove(first)
            second = self.rng.choice(pool)
            
            self.log_pattern(f"RunTwoAdditional({first}, {second})")
            
            self.env.pattern_tokens.insert(0, f"run_additional_{second}")
            self.env.pattern_tokens.insert(0, "gap")
            self.env.pattern_tokens.insert(0, f"run_additional_{first}")
            
        elif token.startswith("run_additional_"):
            idx = int(token.split("_")[-1])
            mapping = {0: "additional_1a", 1: "additional_1b", 2: "n_stroke", 3: "z_stroke", 4: "phase2_sweep_only", 5: "mark_dash_4", 6: "additional_phase2_plus"}
            self.env.pattern_tokens.insert(0, mapping[idx])
            
        elif token == "additional_phase2_plus":
            choice = self.rng.choice([0, 1, 2])
            mapping = {0: "additional_1b", 1: "n_stroke", 2: "z_stroke"}
            self.env.pattern_tokens.insert(0, mapping[choice])
            
        elif token == "additional_1a":
            self.log_pattern("Additional1A")
            self._add_centered_dash(boss_patterns.left_edge2(), (0, 1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            self.env.events.append(self.env._create_gap_event(0.5))
            self._add_centered_dash(boss_patterns.top_edge2(), (1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            self.env.events.append(self.env._create_gap_event(0.5))
            self._add_centered_dash(boss_patterns.right_edge2(), (0, -1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            self.env.events.append(self.env._create_gap_event(0.5))
            self._add_centered_dash(boss_patterns.bottom_edge2(), (-1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "additional_1b":
            self.log_pattern("Additional1B")
            self._add_centered_dash(boss_patterns.horizontal_stripe3(0), (1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            self.env.events.append(self.env._create_gap_event(0.5))
            self._add_centered_dash(boss_patterns.vertical_stripe3(0), (0, 1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "n_stroke":
            self.log_pattern("NStroke")
            reverse = self.rng.random() < 0.5
            if not reverse:
                self._add_centered_dash(boss_patterns.left_edge2(), (0, -1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
                self.env.events.append(self.env._create_gap_event(0.05))
                self.env._add_dash_cast(boss_patterns.diagonal_tr_bl_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (-3, -3), (3, 3), hide_after=True)
                self.env.events.append(self.env._create_gap_event(0.05))
                self._add_centered_dash(boss_patterns.right_edge2(), (0, -1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            else:
                self._add_centered_dash(boss_patterns.right_edge2(), (0, 1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
                self.env.events.append(self.env._create_gap_event(0.05))
                self.env._add_dash_cast(boss_patterns.diagonal_tr_bl_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (3, 3), (-3, -3), hide_after=True)
                self.env.events.append(self.env._create_gap_event(0.05))
                self._add_centered_dash(boss_patterns.left_edge2(), (0, 1), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "z_stroke":
            self.log_pattern("ZStroke")
            reverse = self.rng.random() < 0.5
            if not reverse:
                self._add_centered_dash(boss_patterns.top_edge2(), (1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
                self.env.events.append(self.env._create_gap_event(0.05))
                self.env._add_dash_cast(boss_patterns.diagonal_tr_bl_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (3, 3), (-3, -3), hide_after=True)
                self.env.events.append(self.env._create_gap_event(0.05))
                self._add_centered_dash(boss_patterns.bottom_edge2(), (1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            else:
                self._add_centered_dash(boss_patterns.bottom_edge2(), (-1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
                self.env.events.append(self.env._create_gap_event(0.05))
                self.env._add_dash_cast(boss_patterns.diagonal_tr_bl_3(), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"], (-3, -3), (3, 3), hide_after=True)
                self.env.events.append(self.env._create_gap_event(0.05))
                self._add_centered_dash(boss_patterns.top_edge2(), (-1, 0), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "phase2_sweep_only":
            self.log_pattern("Phase2SweepOnly")
            start_index = self.rng.choice([0, 1, 2, 3])
            bands = [boss_patterns.left_band4(), boss_patterns.bottom_band4(), boss_patterns.right_band4(), boss_patterns.top_band4()]
            dirs = [(0, -1), (1, 0), (0, 1), (-1, 0)]
            for i in range(4):
                idx = (start_index + i) % 4
                warn_time = config.PATTERN_ENTRY_WARNING_SECONDS + (0.2 if i == 0 else 0)
                self._add_centered_dash(bands[idx], dirs[idx], warn_time, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
                if i < 3:
                    self.env.events.append(self.env._create_gap_event(0.5))
            
        elif token == "mark_dash_4":
            self.log_pattern("MarkDash4")
            for i in range(4):
                self.expand_token("mark_dash")
                self.env.events.append(self.env._create_gap_event(0.60)) # afterMarkDashDelay = 0.60
                
        elif token == "mark_dash":
            is_fake = self.current_phase >= 3 and self.rng.random() < 0.5
            display_horizontal = self.rng.random() < 0.5
            actual_horizontal = not display_horizontal if is_fake else display_horizontal
            
            player_cell = self.env.player_cell
            display_cells = boss_patterns.mark_horizontal5(player_cell) if display_horizontal else boss_patterns.mark_vertical5(player_cell)
            actual_cells = boss_patterns.horizontal_stripe3(player_cell[1]) if actual_horizontal else boss_patterns.vertical_stripe3(player_cell[0])
            
            from boss_env import Event
            w_steps = config.seconds_to_steps(0.68)
            d_steps = config.seconds_to_steps(0.15)
            self.env.events.append(Event(PATTERN_IDS["MARK_DASH"], "warning", w_steps, warning_cells=display_cells, future_damage_cells=actual_cells, hide_boss_visible=True))
            
            dash_dir = (1, 0) if actual_horizontal else (0, -1)
            start = boss_patterns.get_centered_dash_start(actual_cells, dash_dir)
            end = boss_patterns.get_centered_dash_end(start, dash_dir)
            self.env.events.append(Event(PATTERN_IDS["MARK_DASH"], "damage", d_steps, damage_cells=actual_cells, commit_start=start, commit_end=end, dash_path_cells=actual_cells, boss_dash_active=True, hide_after=True))
            
        elif token == "final_phase_routine":
            self.log_pattern("FinalPhaseRoutine")
            for i in range(4):
                target = self.env.player_cell
                self.env._add_cast(boss_patterns.hollow_corner_5x5(target), 0.66, 0.15, PATTERN_IDS["LANDING_SLAM"])
                # After slam, it does random dash (if finalSlamUseRandomNoWarningDash) or just original dash. Default is original dash (forward stripe).
                self.env.pattern_tokens.insert(0, "final_original_dash")
                
        elif token == "final_original_dash":
            # Approximated by a stripe dash
            target = self.env.player_cell
            self.env._add_cast(boss_patterns.horizontal_stripe3(target[1]), 0.70, 0.15, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "cast_hollow_corner":
            self.env._add_cast(boss_patterns.hollow_corner_5x5((0,0)), config.PATTERN_ENTRY_WARNING_SECONDS, config.PATTERN_ENTRY_DAMAGE_SECONDS, PATTERN_IDS["PATTERN_ENTRY_DASH"])
            
        elif token == "landing_slam":
            if self.current_phase >= 3:
                cells = boss_patterns.hollow_corner_5x5(self.env.player_cell)
            else:
                cells = boss_patterns.normal_scratch_directional(self.env.player_cell, (0,-1)) # approximated as normal scratch from player cell downwards
            
            self.env._add_cast(
                cells, 
                0.64, # landingWarningTime
                0.15, # landingDamageTime
                PATTERN_IDS["LANDING_SLAM"]
            )
            
        elif token == "gap":
            self.env.events.append(self.env._create_gap_event(0.35))
            
        elif token == "basic_attack_loop":
            self.env.pattern_tokens.insert(0, "basic_rep_step")
            
        elif token == "basic_rep_step":
            if config.RANDOMIZE_BOSS_PATTERNS:
                self.evaluate_phase()
                # Unity's BasicAttackLoop walks to player and scratches, but ends if boss dies or Phase changes.
                # Since we already do evaluate_and_entry in loop, basic_rep_step can just do 1 attack.
                # Actually Unity's BasicAttackLoop does multiple scratches if player is near.
                # Let's keep it simple: just 1 scratch/move, then boss_loop will continue it.
                pass
            
            # Original basic behavior
            from coordinate_mapping import manhattan, normalize_dir, add, clamp_cell
            if manhattan(self.env.player_cell, self.env.boss_logic_cell) > 1:
                delta = (self.env.player_cell[0] - self.env.boss_logic_cell[0], self.env.player_cell[1] - self.env.boss_logic_cell[1])
                direction = normalize_dir(delta)
                self.env.boss_facing = direction
                self.env.boss_logic_cell = clamp_cell(add(self.env.boss_logic_cell, self.env.boss_facing))
                
                from boss_env import Event
                self.env.events.append(Event(
                    pattern_id=PATTERN_IDS["BOSS_MOVE"],
                    kind="move",
                    duration=config.BOSS_MOVE_ONE_CELL_STEPS,
                    commit_start=self.env.boss_logic_cell
                ))
            else:
                delta = (self.env.player_cell[0] - self.env.boss_logic_cell[0], self.env.player_cell[1] - self.env.boss_logic_cell[1])
                if delta != (0, 0):
                    self.env.boss_facing = normalize_dir(delta)
                cells = normal_scratch_directional(self.env.boss_logic_cell, self.env.boss_facing)
                self.env._add_cast(cells, config.NORMAL_SCRATCH_WARNING_SECONDS, config.NORMAL_SCRATCH_DAMAGE_SECONDS, PATTERN_IDS["BASIC_SCRATCH"])
                
                from boss_env import Event
                self.env.events.append(Event(PATTERN_IDS["GAP"], "gap", config.seconds_to_steps(config.NORMAL_SCRATCH_RECOVERY_SECONDS)))
