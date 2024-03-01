# How to use

## Basic usage

https://github.com/cstria0106/OneClickInventory/assets/11474150/3ec366ef-51f6-4d3d-93e8-a9a6c5c60243

1.  Add items to your avatar. Use modular avatar or manual way. Organize your hierarchy like,

    ```
    Avatar
    ├─ Cloths (Inventory)
    │  ├─ Default (Item)
    │  ├─ Cute one (Item)
    │  ├─ Pretty one (Item)
    ├─ Hairs (Inventory)
    │  ├─ Long hair (Item)
    │  ├─ Short hair (Item)
    │  ├─ Cute hair (Item/Inventory)
    │     ├─ Red color (Item)
    │     ├─ Blue color (Item)
    ├─ Accessories (Inventory)
    │  ├─ Glasses (Item)
    │  ├─ Hat (Item)
    ```


2. Put the "Inventory" component in the item you want to use.
3. Add the "Inventory" component to the game object containing the item (parent).
4. Modify the properties of each added component in the inspector.
    - If multiple items cannot be turned on at the same time such as clothes and hair, check "Enable only one item" of the parent object. If they can be turned on at the same time as accessories, check not.
    - Set additional objects to be activated/deactivated, blend shapes to be set, materials to be changed, and additional animations as needed in the item object. For example, if the avatar penetrates clothes, it can be solved by adjusting the avatar's Kisekae (Shrink) blend shape.
    - You can automatically create an icon by pressing "Generate icon" on the item object. Some objects may fail to create icons.
    - When Inventory is nested, (three or more) submenus are created. For example, it can be used for detail settings for items (some parts toggles, palette swaps).
5. Play or upload. Inventory animations and menu will be automatically generated.


## If the avatar penetrates cloths

https://github.com/cstria0106/OneClickInventory/assets/11474150/6ec4d0b1-b523-4968-a188-55543feae4aa

## If you want to change accessories color by cloth

https://github.com/cstria0106/OneClickInventory/assets/11474150/a25c41b2-7d4b-4c07-90a1-a5cc61f96969


