# Making hero models with Meshy

The **Boba Art Kit** gives every slot a matching pastel model. Meshy (meshy.ai) can turn a text prompt into a more detailed 3D model, so you can upgrade the pieces you see most and keep the kit for the rest.

Start with the three pieces players stare at: the **tea bar** (TeaBrewer), the **front counter** (Counter) and **you** (Player).

## 1. Generate

1. Sign in at meshy.ai and open **Text to 3D**.
2. Paste a prompt from the table below, with the style line added to the end so everything matches.
3. Generate, pick the preview you like best, then let it make the full textured model.
4. Download it as **FBX** (keep the textures that come with it).

Before you sell the game, check Meshy's license for your plan to make sure it allows commercial use.

**Style line (add to every prompt):**

> cute stylized low-poly game asset, soft rounded shapes, pastel pink, mint, cream and light wood colors, modern bubble tea shop, single object, no text, no background

| Slot | Prompt |
|------|--------|
| TeaBrewer | bubble tea shop counter with three stainless steel tea dispensers with taps on top, cream cabinet with pink trim, light wood countertop |
| Counter | bubble tea shop front counter with a small cash register, a tip jar and a service bell, blush pink cabinet, light wood countertop |
| ToppingBar | long counter with a row of open tubs of black tapioca pearls, coconut jelly and popping boba behind a small glass sneeze guard, mint green cabinet, wood top |
| TaroStation | bubble tea cup sealing machine on a lavender counter next to a bowl of purple taro paste |
| FruitBar | fruit tea counter with a blender and a bowl of mangoes, strawberries and kiwis, peach colored cabinet, wood top |
| DriveThru | drive-through pickup counter with a pink and white striped awning, mint cabinet, wood top |
| UpgradeDesk | small lavender desk with a tablet on a stand and a little potted succulent |
| Player | chibi barista girl with two hair buns, pink apron over a cream shirt, big head, standing with arms at her sides, facing forward |
| Runner | chibi shop helper wearing a mint cap and mint apron, big head, standing with arms at sides, facing forward |
| Customer | chibi young customer with a bob haircut, yellow sweater and small backpack, big head, standing with arms at sides, facing forward |
| Car | cute toy-like compact car, pastel mint with a pink stripe, rounded shapes |
| Cup | bubble tea cup with milk tea, black tapioca pearls at the bottom, clear dome lid and a thick pink straw |
| Decoration | potted monstera plant in a cream pot |
| Decoration | giant bubble tea cup statue with a cute smiling face |

Characters look best standing with arms down, because the game makes them waddle instead of playing walk animations.

## 2. Bring it into Unity

1. Make a folder per model, for example `Assets/_Game/Art/Meshy/TeaBar`, and drop the FBX and its textures in.
2. Click the FBX in the Project window. In the Inspector's **Materials** tab, click **Extract Textures** and then **Extract Materials** if the model looks white. If it looks pink, see [ART.md](ART.md).
3. Click **Bubble Tea > Art > Select Art Set** and drag the FBX into the matching slot's **Model** field (for decorations, into the **Decorations** list).
4. Click **Bubble Tea > Build Prototype Scene** and press Play.
5. If it faces the wrong way, set the slot's **Rotation Y** to `180` (or `90` / `-90`) and rebuild. Stations should face where you stand.

Running **Build Boba Art Kit** again puts the kit models back in every slot, so after it, drag your Meshy models in again.

## Notes

- Models are scaled to fit the gray box automatically, so the size you generate at doesn't matter.
- A Meshy cup can't change color per drink, so drinks show a colored lid instead. The kit's cup shows the drink's color.
- Commit the `Assets/_Game/Art/Meshy` folder (with its `.meta` files) and the Art Set so the models stay linked.
