# action_space.py

# Actions definition based on Unity rule (10 actions)
ACTION_NAMES = [
    "NONE",
    "UP",
    "DOWN",
    "LEFT",
    "RIGHT",
    "ATTACK",
    "UP_ATTACK",
    "DOWN_ATTACK",
    "LEFT_ATTACK",
    "RIGHT_ATTACK"
]

def decode_action(action_index: int) -> tuple[str, bool]:
    """
    Decodes an integer action into (movement_input, attack_pressed)
    Returns:
        movement_input (str): "NONE", "UP", "DOWN", "LEFT", "RIGHT"
        attack_pressed (bool): True if an attack was pressed in the same frame
    """
    name = ACTION_NAMES[action_index]
    
    if name == "NONE":
        return "NONE", False
    if name == "ATTACK":
        return "NONE", True
        
    parts = name.split("_")
    if len(parts) == 1:
        return parts[0], False
    else:
        return parts[0], True

def get_movement_vector(movement_input: str) -> tuple[int, int]:
    """
    Returns the dx, dy for a movement input string.
    """
    if movement_input == "UP": return (0, 1)
    if movement_input == "DOWN": return (0, -1)
    if movement_input == "LEFT": return (-1, 0)
    if movement_input == "RIGHT": return (1, 0)
    return (0, 0)
