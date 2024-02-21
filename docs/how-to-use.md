# How To Use

![](./images/1.png)
Dress your avatar (use modular avatar or manual way).
Hierarchy must be organized just like

```
- Closet 1
    - Cloth 1
    - Just GameObject
    - Cloth 2
    - ...
- Closet 2
    - Cloth 3
    - Cloth 4
    - Just GameObject
        - Cloth 5
    - ...
```

![](./images/2.png)
Select GameObject containing cloths in hierarchy and add component 'Closet'.
![](./images/3.png)
Setup closet properties in inspector.
'Is unique' option means only one of closet item can be shown at one time.
![](./images/4.png)
Select cloth GameObject and add component 'Closet Item'.
![](./images/5.png)
Setup closet item properties in inspector.
'Default' option means this item is enabled by default.
'Additional objects' option means objects that should be shown together with this cloth.
And you can genreate cloth's icon with clicking button.

Now test your avatar and upload. Closet will be automatically applied to avatar when you play in your Unity editor or upload to VRChat.
