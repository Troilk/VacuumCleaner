This project is an environment for experimenting with toy AI problem - Vacuum cleaner world, originally
described in book [Artificial Intelligence: A Modern Approach](http://aima.cs.berkeley.edu/). It's actually
a C# adaptation of [this C++ project](http://web.ntnu.edu.tw/~tcchiang/ai/Vacuum%20Cleaner%20World.htm) with
more user friendly interface.

[Video of environment usage](http://www.youtube.com/watch?v=xZDnrxhIxrM)
Problem description
======
In the simple world, the vacuum cleaner agent has a bump sensor and a dirt sensor so that it knows if it
hit a wall and whether the current tile is dirty. It can go left, right, up and down, clean dirt, and idle.
A performance measure is to maximize the number of clean rooms over a certain period and to minimize energy
consumption. The geography of the environment is unknown. At each time step, each room has a certain chance of increasing 1 unit of dirt.  

* Prior knowledge

------
1. The environment is a square, surrounded with walls.
2. Each cell is either a wall or a room.
3. The walls are always clean.
4. The agent cannot pass through the wall.
5. The agent can go north, south, east, and west. Each move costs 1 point of energy.
6. The agent can clear dirt, each time decreasing 1 unit of dirt. Each cleaning costs 2 point of energy.
7. The agent can stay idle, costing no energy.  

* Performance measure

------
Given a period T, the goal is 

1. Minimize the sum of amount of dirt in all rooms over T.  
2. Minimize the consumed energy.

Agents
======
The project contains 3 default agents:  

* RandomAgent - performs random actions on each iteration.  
* ModelAgent. 
Agent works in 2 stages:

1. Map discovery. (2n - 1)x(2n - 1) map is created, where n is width/height of real map and it is assumed that agent is at the center of this map.
Agent chooses among neighboring tiles cell which he has not visited yet (call it «black») and moves to it. If current tile has no uninvestigated neighbors
(it's «white»), agent searches for shortest path to nearest «grey»(visited tile but with uninvestigated neighbors) tile, using A* algorithm. Manhattan distance
to nearest «grey» tile from current tile is used as a heuristic for algorithm. If map has no «grey» tiles left, then all accessible tiles are investigated
and first stage of algorithm is finished. All uninvestigated tiles are marked as walls to prevent problems of algorithm on the 2 stage. Left top tile coordinates
are determined and map is trimmed to smaller (n * n) map.

2. Regular map traverse. Simple greedy algorithm is used. Agent moves to neighboring tile which was not visited for longest time. It may decide to idle
using approximation of dirt respawn time. To approximate time of dirt respawn agent sums all time intervals between dirt clearing and divides it by count
of dirt collections.  

* ModelAgentNoIdle - behaves as the previous one but is not trying to predict when to idle.

Default agents work plots
------
<img src="/Plots/energy_1.png" width="400" alt="Energy map_1">
<img src="/Plots/dirt_1.png" width="400" alt="Dirt map_1">  

<img src="/Plots/energy_2.png" width="400" alt="Energy map_2">
<img src="/Plots/dirt_2.png" width="400" alt="Dirt map_2">  

<img src="/Plots/energy_3.png" width="400" alt="Energy map_3">
<img src="/Plots/dirt_3.png" width="400" alt="Dirt map_3">  

<img src="/Plots/energy_4.png" width="400" alt="Energy map_4">
<img src="/Plots/dirt_4.png" width="400" alt="Dirt map_4">  

Renderers
======
For displaying map and agent 2 default renderers are available:  

* 2D renderer - renders classic 2D tile map
* 3D renderer - renders 3D, textured tile map

System requirements
======
Windows  
[XNA 4.0 Refresh](http://www.microsoft.com/en-us/download/details.aspx?id=27599)  
Microsoft Visual Studio 2010  

XNA does not officially support Visual Studio 2012. You can try out [this tutorial](http://ryan-lange.com/xna-game-studio-4-0-visual-studio-2012/)
to install XNA on VS 2012 but you will need 2010 version anyway.

Used libraries/assets
======
[XNA 4.0 Refresh](http://en.wikipedia.org/wiki/Microsoft_XNA)  
[Neoforce Controls](http://neoforce.codeplex.com/) ([License](http://neoforce.codeplex.com/license))  

Some textures from [CG Textures](http://www.cgtextures.com/)