# Fault in our stars
## Technical challenge

Hello and welcome to my submission for the technical challenge.  
To get started, clone the repository and open it in unity.  
The tool is accessible via Window -> Star Creator  
Doc is [here](Assets/Tools/Star%20Creator/Doc.md)

### Techical writeups
It was an interesting project, it's always fun to start back on an empty clean codebase.  
It was my first time with uitoolkit, but thanks to my front end web experience I wasn't too lost.  
After using uitoolkit and uibuilder for two days I do not wish to go back to the old Unity UI ^^  

After the facts I realize that I addressed this project in a somewhat different manner than I would have addressed a usual tool request.  
Since it's a technical challenge I empathise on the "show your skill and editor knowledge", making me not take necessarily the easiest solutions.

For example I used Unity's JsonUtility for sereliazation which does not support array or dictionary.  
This leads me to give up on the .json extention for a .starPresets which contains one json per preset/per line + the preset name.  
In a normal project I'd have just used Newtonsoft that I probably had already installed anyway.  

Another unnecessary thing was overly relying on AssetDatabase to ensure a folder exist instead of a simple SystemIO CreateDirectory.  

Also, due to it being a technical challenge is that I was less worried about future-proofing it which helps explain my choice for preset management which is simply using star prefabs in a folder.  
This solution ticks all the requirement of the technical challenge but in a real life situation, the tool would probably have evolved to handle more complex preset that would not be saved purely in a star prefab.  

Finally a point I forgot to watch out for is proper commit history.  
Usually for me this would have just been a merge request from a fork or feature branch and in the workflows I work with we squash commit at merge.
