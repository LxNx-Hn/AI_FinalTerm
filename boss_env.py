import random
from typing import Optional, List, Tuple
import config
from coordinate_mapping import clamp_cell, manhattan, add, normalize_dir, is_valid_cell
from player_rules import get_player_attack_cells
from boss_patterns import (
    PATTERN_IDS, normal_scratch_directional, enhanced_scratch_directional,
    forward_stripe3_from_cell, hollow_corner_5x5, mark_horizontal5, horizontal_stripe3,
    pattern_dash_start, pattern_dash_end, directional_edge_cell
)
from action_space import decode_action, get_movement_vector

class Event:
    def __init__(
        self,
        pattern_id: int,
        kind: str,
        duration: int,
        warning_cells: Optional[List[Tuple[int, int]]] = None,
        damage_cells: Optional[List[Tuple[int, int]]] = None,
        future_damage_cells: Optional[List[Tuple[int, int]]] = None,
        commit_start: Optional[Tuple[int, int]] = None,
        commit_end: Optional[Tuple[int, int]] = None,
        dash_path_cells: Optional[List[Tuple[int, int]]] = None,
        boss_dash_active: bool = False,
        hide_boss_visible: bool = False
    ):
        self.pattern_id = pattern_id
        self.kind = kind # "warning", "damage", "move", "gap"
        self.duration = duration
        self.remaining = duration
        self.warning_cells = warning_cells or []
        self.damage_cells = damage_cells or []
        self.future_damage_cells = future_damage_cells or []
        self.commit_start = commit_start
        self.commit_end = commit_end
        self.dash_path_cells = dash_path_cells or []
        self.boss_dash_active = boss_dash_active
        self.hide_boss_visible = hide_boss_visible

class BossEnv:
    def __init__(self, seed: Optional[int] = None):
        self.rng = random.Random(config.SEED if seed is None else seed)
        self.reset()

    def reset(self):
        self.step_count = 0
        self.player_cell = config.PLAYER_START_CELL
        self.player_facing = config.PLAYER_START_FACING
        self.player_hp = config.PLAYER_MAX_HP
        self.attack_cooldown = 0
        self.move_cooldown = 0

        self.boss_logic_cell = config.BOSS_START_CELL
        self.boss_facing = config.BOSS_START_FACING
        self.boss_hp = config.BOSS_MAX_HP
        self.boss_phase = config.PHASE1
        
        self.boss_visible_flag = True
        self.boss_visible_cell = self.boss_logic_cell
        self.boss_hurtbox_cells = [self.boss_logic_cell]
        
        self.events: List[Event] = []
        self.current_event: Optional[Event] = None
        self.pattern_tokens: List[str] = ["basic_rep"]
        
        self.metrics = {
            "boss_damage_dealt": 0,
            "boss_kill": False,
            "death": False,
            "damage_during_dash_count": 0,
            "no_hit_damage_during_dash_count": 0,
            "damage_tile_entries": 0,
            "hit_while_attack_count": 0,
            "hit_while_moving_count": 0,
            "hit_while_staying_count": 0
        }
        
        return self._get_obs()

    def _get_obs(self):
        # We will separate state encoding into state_encoder.py
        # For now, just return self reference.
        return self

    def step(self, action_index: int):
        self._ensure_event()
        self._start_current_event()
        
        boss_hp_before = self.boss_hp
        movement_input, attack_pressed = decode_action(action_index)
        
        hit_player = False
        attack_success = False
        off_map = False
        
        # Player Move
        if movement_input != "NONE":
            dir_vec = get_movement_vector(movement_input)
            new_cell = add(self.player_cell, dir_vec)
            self.player_facing = dir_vec  # Always update facing
            if is_valid_cell(new_cell[0], new_cell[1]):
                self.player_cell = new_cell
            else:
                off_map = True
                
        # Player Attack
        if attack_pressed and self.attack_cooldown <= 0:
            attack_cells = get_player_attack_cells(self.player_cell, self.player_facing)
            self.attack_cooldown = config.ATTACK_COOLDOWN_STEPS
            
            # Check intersection with boss hurtbox
            hit_any = any(c in self.boss_hurtbox_cells for c in attack_cells)
            if hit_any:
                self.boss_hp = max(0, self.boss_hp - 1)
                attack_success = True
                if self.current_event and self.current_event.boss_dash_active:
                    self.metrics["damage_during_dash_count"] += 1
                    
        # Apply current event damage
        in_warning_tile = False
        in_damage_tile = False
        in_next_damage_risk = False
        
        if self.current_event:
            if self.current_event.kind == "warning":
                in_warning_tile = self.player_cell in self.current_event.warning_cells
                in_next_damage_risk = self.player_cell in self.current_event.future_damage_cells
            elif self.current_event.kind == "damage":
                in_damage_tile = self.player_cell in self.current_event.damage_cells
                if in_damage_tile:
                    self.player_hp -= 1
                    hit_player = True
        
        # Tick time
        self.step_count += 1
        if self.attack_cooldown > 0:
            self.attack_cooldown -= 1
            
        if self.current_event:
            self.current_event.remaining -= 1
            if self.current_event.remaining <= 0:
                self.current_event = None
                
        # Evaluate Phase
        boss_damage = boss_hp_before - self.boss_hp
        self.metrics["boss_damage_dealt"] += boss_damage
        
        done = False
        if self.boss_hp <= 0:
            self.metrics["boss_kill"] = True
            done = True
        if self.player_hp <= 0:
            self.metrics["death"] = True
            done = True
        if self.step_count >= config.MAX_STEPS:
            done = True
            
        reward = self._calculate_reward(boss_damage, hit_player, in_warning_tile, in_damage_tile, in_next_damage_risk, done)
        return self._get_obs(), reward, done, {}
        
    def _calculate_reward(self, boss_damage, hit_player, in_warning, in_damage, in_risk, done):
        r = config.REWARDS["step_penalty"]
        if boss_damage > 0:
            r += config.REWARDS["boss_hp_damage_reward"] * boss_damage
        if in_warning:
            r += config.REWARDS["warning_tile_penalty"]
        if in_risk:
            r += config.REWARDS["next_damage_risk_penalty"]
        if in_damage:
            r += config.REWARDS["current_damage_tile_penalty"]
        if hit_player:
            r += config.REWARDS["hit_penalty"]
        if done:
            if self.boss_hp <= 0:
                r += config.calculate_boss_kill_reward(config.MAX_STEPS - self.step_count)
            elif self.player_hp <= 0:
                r += config.REWARDS["death_penalty"]
        return r

    def _ensure_event(self):
        while not self.current_event and not self.events and self.pattern_tokens:
            self._expand_token(self.pattern_tokens.pop(0))

    def _start_current_event(self):
        if not self.current_event and self.events:
            self.current_event = self.events.pop(0)
            # Update boss visual and hurtbox state
            ev = self.current_event
            if ev.kind == "move" and ev.commit_start is not None:
                self.boss_logic_cell = ev.commit_start
                self.boss_visible_cell = self.boss_logic_cell
                self.boss_hurtbox_cells = [self.boss_logic_cell]
            
            if ev.hide_boss_visible:
                self.boss_visible_flag = False
                self.boss_visible_cell = None
            else:
                self.boss_visible_flag = True
                self.boss_visible_cell = self.boss_logic_cell
                
            if ev.boss_dash_active and ev.dash_path_cells:
                self.boss_hurtbox_cells = ev.dash_path_cells
            elif not ev.boss_dash_active:
                self.boss_hurtbox_cells = [self.boss_logic_cell]

    def _expand_token(self, token: str):
        if token == "basic_rep":
            self.pattern_tokens.insert(0, "basic_rep_step")
        elif token == "basic_rep_step":
            if manhattan(self.player_cell, self.boss_logic_cell) > 1:
                delta = (self.player_cell[0] - self.boss_logic_cell[0], self.player_cell[1] - self.boss_logic_cell[1])
                direction = normalize_dir(delta)
                self.boss_facing = direction
                self.boss_logic_cell = clamp_cell(add(self.boss_logic_cell, self.boss_facing))
                self.events.append(Event(
                    pattern_id=PATTERN_IDS["BOSS_MOVE"],
                    kind="move",
                    duration=config.BOSS_MOVE_ONE_CELL_STEPS,
                    commit_start=self.boss_logic_cell
                ))
                self.pattern_tokens.insert(0, "basic_rep_step")
            else:
                delta = (self.player_cell[0] - self.boss_logic_cell[0], self.player_cell[1] - self.boss_logic_cell[1])
                if delta != (0, 0):
                    self.boss_facing = normalize_dir(delta)
                cells = normal_scratch_directional(self.boss_logic_cell, self.boss_facing)
                self._add_cast(cells, config.NORMAL_SCRATCH_WARNING_SECONDS, config.NORMAL_SCRATCH_DAMAGE_SECONDS, PATTERN_IDS["BASIC_SCRATCH"])
                self.events.append(Event(PATTERN_IDS["GAP"], "gap", config.seconds_to_steps(config.NORMAL_SCRATCH_RECOVERY_SECONDS)))
        elif token == "pattern_entry":
            pass # TODO: Implement Full Boss Flow

    def _add_cast(self, cells, warning_sec, damage_sec, pattern_id, hide_boss=False):
        w_steps = config.seconds_to_steps(warning_sec)
        d_steps = config.seconds_to_steps(damage_sec)
        self.events.append(Event(pattern_id, "warning", w_steps, warning_cells=cells, future_damage_cells=cells, hide_boss_visible=hide_boss))
        self.events.append(Event(pattern_id, "damage", d_steps, damage_cells=cells, hide_boss_visible=hide_boss))
