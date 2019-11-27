# MindCraft
Simple endless MineCraft-like world generation in Unity 


## Reinvent the wheel
Nothing i would enjoy doing from scratch, watching already done wheels and looking for improvements seems much more fun so I looked in some papers, tutorials and forums and found that there is quite big consensus on general technical solution - World is split to chunks, each chunk is one game object rendering its own mesh (or meshes). Not specifically to voxel game, terrain generation is powered by noise functions like perlin, simplex or cellular important part for my case here is that function has deterministic output so world is always same given that same seed was used. I started with this basic but very well done [tutorial series](https://www.youtube.com/channel/UC3Ej26l1kXBPIq0fEEMwxQw) made by [b3ags](https://www.youtube.com/channel/UC3Ej26l1kXBPIq0fEEMwxQw)  (you can definitely tell comparing episode titles and couple of first commits in repository:) but i started branching from there quite soon.


## Architecture

Most important thing to change was that i needed to keep map data completely separate and independent on chunk and its rendering logic - this is generaly good aproach for several reasons but i wanted to use new unity Jobs system where data architecture is crucial to make that work effectively.

Other thing is I am not really fan of [monobehaviour tyranny](https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=1&cad=rja&uact=8&ved=2ahUKEwjopPiWgIjmAhUCYcAKHW2NDBAQyCkwAHoECAoQBA&url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3D6vmRwLYWNRo&usg=AOvVaw2e07Zor8YqbD7lme3cwyWZ)  and i have my own framework and also toolset derived from it to works with Strange IoC which is meant to tackle exactly this kind of problems.
 
 
## Performance vs Jobs

When we leave alone initial world generation which can easilly be dealth with, biggest performance hit during game play is when player cross a border between two chunks what triggers generation of several new chunks. 
When i did first round of optimizations which helped me to cut time needed for rendering chunks in half, there was still huge noticeable lag everytime player crossed chunk border. As this task consist of doing same operation on many entities over and over again it was quite clear this is perfect task for Unity Jobs system.

Generating the map data for given chunk, and generating mesh from that data are two completely separate tasks that needs a bit different approach each

#### 1) Map Data 

Whenever we need to generate several chunks of map data, we first determine which exact chunks we need and retrieve its coordinates.
then we will schedule array of Jobs and directly call Jobhandle.CompleteAll() which fill force main thread to wit until all tasks are done. This is needed as we always need to have all data fresh before generating visuals, apllying physics / player input to player character and many other thigs, Luckilly enough, to generate one voxel we need only to know its position and we also know where in the result array that voxel will end up - this makes it eally easy to implement this as Parallel job - we let Jobs sytem decide when it will deal with each voxel and do that in paralell which results in huge time saving

#### 1) Chunk Mesh 

After we have all needed data generated, we can generate actual meshes, while we can't paralelize this task that well as voxel data generation, we don't need it solved in same callstack or even frame. We just schedule job per chunk mesh and update given chunk whenever given job is ready. This means virtually no FPS drop regardless of how much mesh data we need to generate.

## "Endlessness"

Whole world is endless only virtually, there is several restrictions to its real endlessness.

**1) Unity internal world size limit** - that is theoretically max/min float value but advice from unity guys is to not go over 100 000 in any axis as float is not that precise enough at that point. Simple solution to this is to keep player in the midle of world and move the world around him instead.

**2) Player changed world data** - Generated chunk can be thrown away an regenerated again completely identical at will, but we need to keep whatever changes player did. One completely player edited chunk currently takes 8 kilobytes in memory. Whole area in player view takes 4 Megabytes so that would need quite some work with current computers to reach a memory limit, but with more data than 1 byte possibly stored per voxel and possible multiplayer game play, this is something to take it consideration - If this would start to be an issue first option is definitely saving distant chunks on disk and reloading them when needed. This would make this issue even more virtual

**3) World variability** - Procedurally generated World is endless but it can get boring quckly! Having really rich set of Biomes with good rules that make moving trough each of them fun for is key to not feel going trough same thing over and over again.

## TODO's, TODO's Everywhere

Couple Notes and of things that didin't make it to the game (yet):

- **Lighting!** Without proper lighting game looks blend and flat. This is gonna be achieved by custom shaders and data for that should be generated together with chunk mesh itself
- **Visibility distance** - Current cliping plane solution to visibility range is lame and ugly. This should be handled by shaders turning down visibility gradualy past certain point with some fog and nice skybox we should get much nicer result.
- **Water** Adding Water to the world can be achieved simply by filling every air voxel with water voxel in chunk mesh itself those voxels would be just generated in its own submesh. With specific shader wor that mesh we can easily create flowing effect by moving UVs over time.
- **Mesh Optimization** After Jobifiyng chunk generation we don't save triangles that does not need to be drawn between chunks - that is as easy as providing each mesh generating job also adjacent chunks map data and checking against them.


--

Knowledge Sources:

[Sebastian Lague channel](https://www.youtube.com/channel/UCmtyQOKKmrMVaKuRXz02jbQ)

[b3ags unity minecraft tutorial series](https://www.youtube.com/channel/UC3Ej26l1kXBPIq0fEEMwxQw)

[Vox game developer's site](https://sites.google.com/site/letsmakeavoxelengine/)

Used Assets

[Kenney Voxel pack](https://www.kenney.nl/assets/voxel-pack)

[Kenney UI pack](https://www.kenney.nl/assets/ui-pack)




 
  




