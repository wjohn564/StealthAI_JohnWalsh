# StealthAI_JohnWalsh
This project is a small 3D stealth game built in Unity as part of my final assessment for CS4096 - ARTIFICIAL INTELLIGENCE FOR GAMES 2025/6.
The project showcases a single patrolling enemy that can see, hear, and chase the player.

**Controls**
- Move: WASD / Arrow Keys  
- Sprint: Left Shift  
- If the enemy reaches you, the scene restarts.

**AI**
- Patrols between waypoints (NavMeshAgent)  
- Vision: FOV + line-of-sight raycast  
- Hearing: short radius → go investigate last known position  
