# Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior

Welcome to the **Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior** repository. This system simulates dynamic pedestrian movement by integrating various social and physical forces. Agents will:

- Steer toward their goals.
- Avoid collisions.
- Form natural groups.

For a detailed overview and demo, visit the [Project Page](https://reiyaitatani.github.io/SocialCrowdSimulation/).

<img src=".github/media/collision_avoidance_system.png" alt="Collision Avoidance System" width="300"/>

![Collision Avoidance Demo](.github/media/collision_avoidance.gif)

---

## Prerequisites

1. **Unity 2021.2 or Newer**  
   This project requires Unity 2021.2 or later. Compatibility is not guaranteed on older versions.

2. **Motion Matching Package**  
   You need to add the Motion Matching package to your Unity project. Please see [JLPM22's Motion Matching GitHub repository](https://github.com/JLPM22/MotionMatching) for detailed instructions on setting it up.

---

## Installation

1. **Open Unity Editor**  
   Launch Unity (version 2021.2 or newer).

2. **Open Package Manager**  
   - Navigate to **Window > Package Manager**.

3. **Add Package via Git URL**  
   - Click the **Add (+)** button, then select **Add package by git URL...**.
   - Enter the following URL:  
     ```
     https://github.com/ReiyaItatani/SocialCrowdSimulation.git?path=Assets/com.reiya.socialcrowdsimulation
     ```
   - Click **Add** to begin the installation.

>**Note:** All sample scenes are configured for the **Universal Render Pipeline (URP)**. If you are using a different render pipeline, conversion of the scenes may be necessary.

---

## Quick Start: Demo Scene Setup

All the necessary setup components are included in the demo scenes. Below is a step-by-step guide:

### 0. Prepare a Humanoid Character for Social Crowd Simulation

1. **Prefab Creation**  
   - In the **CollisionAvoidance** tab, click **PrefabCreator**. A window with setup options will appear.  
   - Everything required for setup is located in the `Packages/SocialCrowdSimulation/Sample/QuickStart/PrefabCreator` folder.
   - Drag and drop a Humanoid Character, and it will automatically create an Agent in the **Resources** folder.

<img src=".github/media/PrefabCreator.png" alt="Prefab Creator" width="600"/>

2. **MicroSoftRocketBoxAvatar or AvatarSDK**  
   - If you **are not** using either MicroSoftRocketBoxAvatar or AvatarSDK avatars, **uncheck** the “Yes” box related to these avatars.  
     - This prevents blend shape scripts (associated with those specific avatars) from attaching incorrectly.
   - If using **MicroSoftRocketBoxAvatar**, open **CollisionAvoidance** tab > **Target Framework** > **RocketBox Avatar**.  
   - If using an **Avatar from Avatar SDK**, open **CollisionAvoidance** tab > **Target Framework** > **Avatar SDK**.  
   - Correctly setting this ensures that any blend shape functionality works as intended.
<img src=".github/media/TargetFramework.png" alt="Target Framework" width="600"/>

### 1. Define the Crowd to be Spawned in the Scene

1. **Create an AgentList**
   - In the **Project** window, right-click and select **Create > SocialCrowdSimulation > AgentList** to create a new **ScriptableObject**, or use the example found in `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator`.
2. **Add Agents**
   - The **AgentList** manages who will be spawned:
     - **Individual**: An agent that walks alone.
     - **Group**: Agents in a group walk together.
   - **maxSpeed** and **minSpeed** define individual agent speed ranges.
   - **Group Names** must be unique. Do not use “Individual” as a group name. A group must have at least two members (2–3 recommended).

<img src=".github/media/AgentList.png" alt="Agent List" width="600"/>

### 2. Create the GameObjects Required for Avatar Creation

1. **Automatically Create Required GameObjects**  
   - In the **CollisionAvoidance** tab, click **Create AvatarCreator**.
   - This action adds necessary tags (**Agent**, **Group**, **Wall**, **Obstacle**) and creates two GameObjects in your Scene:
     - **OVRLipSyncObject**: Required for lip sync functionality.
     - **AgentCreator**: Contains:
       - **AgentManager**: A script for changing parameters of spawned avatars collectively.
       - **AvatarCreatorQuickGraph** (or similarly named): A script for spawning avatars in the Scene.
<img src=".github/media/AvatarCreation.png" alt="Avatar Creator" width="600"/>

### 3. Path Setup and Agent Instantiation

#### 3.0 Set Up Paths

- Use the examples under `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator`.  
- Place one of the **PathGraph_Example** assets in your scene.  
- Paths are defined by connecting nodes, allowing agents to know where to walk. 

#### 3.1 Instantiate Agents in the Scene

1. **Configure the AvatarCreatorQuickGraph**  
   - Assign your **AgentList** (created in Step 1) or use the example under `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator` to `AvatarCreatorQuickGraph`.
   - Assign the **Path** (the one you placed in the scene) to `AgentCreator`.
2. **Spawn Settings**  
   - **SpawnRadius**: Controls how scattered agents will be when they appear.  
   - **SpawnMethod**: 
     - **OnNode**: Spawns avatars around selected nodes.  
     - **OnEdge**: Spawns avatars on the paths (edges) between nodes.
3. **Spawn the Avatars**  
   - Click **Instantiate Avatar** to create avatars in your scene.
<img src=".github/media/AvatarCreation2.png" alt="Avatar Creator" width="600"/>

### 4. **Run the Simulation**  
   - Press **Play**. The avatars should begin moving according to the defined rules.
---

## Adding a First-Person Camera Player

You can include a first-person player in your scene using the following steps:

### 1. Open the "Create Player" Window

- In the Unity Editor, open the **Create Player** window (from the CollisionAvoidance tab or wherever the plugin menu is located).

### 2. Configure Motion Matching Data

- In the **Motion Matching Data** field, select a `.asset` file from  
  `Packages/SocialCrowdSimulation/Sample/QuickStart/ForPlayerCreator/MotionMatchingData`.

### 3. Set a Humanoid Avatar

- For the **Humanoid Avatar** field, choose a humanoid rig.

### 4. Create the Player

- Click **CreatePlayer** to generate a first-person camera player in your scene.

### 5. Player Movement

- The newly created player can be controlled with **WASD** keys.
- It uses the **SpringCharacterController**, which you can learn more about in the Motion Matching documentation.
- For detailed information on the `SpringCharacterController`, refer to the [Motion Matching documentation](https://jlpm22.github.io/motionmatching-docs/basics/character_controller/).

![Create Player Demo](.github/media/create_player.gif)
---

## Contributions

We encourage contributions and inquiries—please **open an issue** or submit a **pull request** if you wish to collaborate or have questions.

---

## Citation

If you find this work beneficial, kindly attribute it to the authors or cite the following publication:
```bibtex
@inproceedings{10.1145/3677388.3696337,
  author    = {Itatani, Reiya and Pelechano, Nuria},
  title     = {Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior},
  booktitle = {Proceedings of the 17th ACM SIGGRAPH Conference on Motion, Interaction, and Games (MIG '24)},
  year      = {2024},
  doi       = {10.1145/3677388.3696337}
}
```


