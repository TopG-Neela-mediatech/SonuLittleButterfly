# Sonu's Little Butterfly - Audio Script

Kids game, ages 3-6. The sheet next to this file (`Sonu's Little Butterfly - Audio Script.xlsx`) is the recording list.
Its columns are the same as every Playschool game: `voiceover_text`, `emotion`, `activity_name`, `voiceover_title`.

- **`voiceover_title` is the key.** `RuntimeAudioLoader` finds each clip by that exact string, so the clip names must match the sheet.
- **`activity_name`** is `sonulittlebutterfly` for every row.
- **No line says a level number.**

---

## 1. How the audio is wired (same structure as BuildABot / Ambulance / Tug of War)

Scene hierarchy:

```
Scripts
  GameManager              GameManager.Instance.SoundManager points at the SoundManager below
  LevelManager
  SoundManager             SoundManager.cs (tmkoc.butterfly)
    BGMSource              AudioSource: loop on, play on awake off (background music clip goes here)
    SFXSource              AudioSource: loop off, play on awake off (one-shot sound effects)
  TutorialController
```

`SoundManager` has three parts:

| Part | What it is |
|---|---|
| `audioMapper` (`ButterflyAudioMapper`) | Every voice key, grouped by when it plays. The defaults in code match the sheet. The scene builder writes them into the scene. |
| `bgmSource` / `sfxSource` | The two AudioSources above. |
| `sfxData` (`SfxType` + clip + volume) | The sound-effect clips, set in the Inspector. |

- **Voice lines:** they play through `RuntimeAudioLoader.Instance.PlayRuntimeAudio(key)`. A new line stops the one before it. Every voice method returns the clip length, or -1 if the key is missing, and the game carries on silently.
- **Story slides:** they use the returned length to time each slide.

---

## 2. When each voice line plays

| Key(s) | Plays when | Called from |
|---|---|---|
| `story1`-`story3` | Intro story (first time the game starts), one per slide: butterfly, Sonu and Madhavi, the tree | `LevelManager` -> `PlayStorySlide(i)` |
| `water_intro1` | Level 1 (water the leaves) starts | `PlayLevelIntro(Water)` |
| `leaf_intro1` | Level 2 (feed the caterpillar) starts | `PlayLevelIntro(Leaf)` |
| `twig_intro1` | Level 3 (grow the branch) starts | `PlayLevelIntro(Twig)` |
| `flower_intro1` | Level 4 (water the flowers) starts | `PlayLevelIntro(Flower)` |
| `goal1`, `goal2` | A goal item lands in the garden (random) | `ButterflyLevelController` -> `PlayGoalDelivered()` |
| `wrong1`, `wrong2` | Merge that doesn't work (random, at most every 4 s) | `ButterflyLevelController` -> `PlayWrongMove()` |
| `idle1`, `idle2` | Tutorial hint after the kid has been idle (random) | `TutorialController` -> `PlayIdle()` |
| `egg_laid`, `egg_hatch` | End of level 1: the egg is laid, then it hatches | `GardenView` -> `PlayGardenMoment()` |
| `caterpillar_branch` | End of level 2: the caterpillar crawls onto the branch | `GardenView` |
| `chrysalis_form`, `chrysalis_rest`, `chrysalis_move`, `butterfly_emerge` | End of level 3: chrysalis forms, rests, wiggles, the butterfly comes out | `GardenView` |
| `butterfly_nectar` | End of level 4: the butterfly drinks nectar | `GardenView` |
| `outro1`, `outro2` | Level complete, after the garden animation (random) | `LevelManager` -> `PlayLevelOutro()` |
| `stage1_end1`-`stage4_end2` | Story between levels, two slides each | `PlayStageStory(stage, i)` |
| `final_outro` | Game complete, with the final celebration screen | `PlayFinalOutro()` |

**Lengths:** keep `goal*`, `wrong*` and `idle*` under 2 s. The garden-moment lines play during fixed-length animations, so keep them under 3 s. Story and intro lines can be longer.

**Voice direction:** a warm, cheerful big-sister narrator. Smiling, clear and slow for little ears. Never scolding, even on a wrong move.

### Copy-paste list of every clip name (32)

```
story1  story2  story3
water_intro1  leaf_intro1  twig_intro1  flower_intro1
goal1  goal2  wrong1  wrong2  idle1  idle2
egg_laid  egg_hatch  caterpillar_branch
chrysalis_form  chrysalis_rest  chrysalis_move  butterfly_emerge  butterfly_nectar
outro1  outro2
stage1_end1  stage1_end2  stage2_end1  stage2_end2  stage3_end1  stage3_end2  stage4_end1  stage4_end2
final_outro
```

---

## 3. Music (BGMSource clip)

| File name | Plays when | Direction | Loop |
|---|---|---|---|
| `mus_garden` | From the first level until the game ends | Gentle, happy garden tune: ukulele, light marimba, soft birds. Calm enough for thinking. | Yes |

## 4. Sound effects (`sfxData`, `SfxType`)

| SfxType | Suggested file | Trigger | Description |
|---|---|---|---|
| Move | `sfx_move` | Item moved to an empty cell | Soft pop / slide |
| Merge | `sfx_merge` | Two items merge | Bright sparkle "bling" |
| Wrong | `sfx_wrong` | Merge that doesn't work | Gentle "boop", never harsh |
| Deliver | `sfx_deliver` | Goal item lands in the garden | Magical whoosh + chime |
| StageComplete | `sfx_stage_complete` | Level won (garden animation starts) | Short happy fanfare |
| Celebrate | `sfx_celebrate` | Final celebration screen | Big party fanfare + cheer |

Until the clips are set in the Inspector, these effects are silent. The game does not throw errors.
