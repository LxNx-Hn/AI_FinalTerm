import torch
import torch.nn as nn
import torch.optim as optim
import random
import numpy as np
from collections import deque

class QNetwork(nn.Module):
    def __init__(self, state_dim, action_size):
        super(QNetwork, self).__init__()
        self.fc1 = nn.Linear(state_dim, 256)
        self.fc2 = nn.Linear(256, 256)
        self.fc3 = nn.Linear(256, action_size)

    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        return self.fc3(x)

class DQNAgent:
    def __init__(self, state_dim, action_size=10, device="cpu", 
                 lr=1e-4, gamma=0.99, batch_size=128, replay_buffer_size=100000, 
                 target_update_interval=1000, epsilon_start=1.0, epsilon_min=0.03, epsilon_decay=0.9995):
        self.state_dim = state_dim
        self.action_size = action_size
        self.device = device
        
        self.q_network = QNetwork(state_dim, action_size).to(device)
        self.target_network = QNetwork(state_dim, action_size).to(device)
        self.target_network.load_state_dict(self.q_network.state_dict())
        self.target_network.eval()
        
        self.optimizer = optim.Adam(self.q_network.parameters(), lr=lr)
        
        self.memory = deque(maxlen=replay_buffer_size)
        self.batch_size = batch_size
        self.gamma = gamma
        self.target_update_interval = target_update_interval
        
        self.epsilon = epsilon_start
        self.epsilon_min = epsilon_min
        self.epsilon_decay = epsilon_decay
        self.step_count = 0

    def get_action(self, state, exploit=False):
        if not exploit and random.random() < self.epsilon:
            return random.randint(0, self.action_size - 1)
            
        state_t = torch.FloatTensor(state).unsqueeze(0).to(self.device)
        with torch.no_grad():
            q_values = self.q_network(state_t)
        return torch.argmax(q_values).item()

    def update(self, state, action, reward, next_state, done):
        self.memory.append((state, action, reward, next_state, done))
        
        if len(self.memory) < self.batch_size:
            return
            
        batch = random.sample(self.memory, self.batch_size)
        states, actions, rewards, next_states, dones = zip(*batch)
        
        states_t = torch.FloatTensor(np.array(states)).to(self.device)
        actions_t = torch.LongTensor(actions).unsqueeze(1).to(self.device)
        rewards_t = torch.FloatTensor(rewards).unsqueeze(1).to(self.device)
        next_states_t = torch.FloatTensor(np.array(next_states)).to(self.device)
        dones_t = torch.FloatTensor(dones).unsqueeze(1).to(self.device)
        
        q_values = self.q_network(states_t).gather(1, actions_t)
        with torch.no_grad():
            max_next_q = self.target_network(next_states_t).max(1)[0].unsqueeze(1)
            target_q = rewards_t + (1 - dones_t) * self.gamma * max_next_q
            
        loss = nn.functional.smooth_l1_loss(q_values, target_q)
        
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()
        
        self.step_count += 1
        if self.step_count % self.target_update_interval == 0:
            self.target_network.load_state_dict(self.q_network.state_dict())

    def decay_epsilon(self):
        self.epsilon = max(self.epsilon_min, self.epsilon * self.epsilon_decay)

    def save(self, filepath):
        torch.save(self.q_network.state_dict(), filepath)

    def load(self, filepath):
        self.q_network.load_state_dict(torch.load(filepath, map_location=self.device, weights_only=True))
        self.target_network.load_state_dict(self.q_network.state_dict())
