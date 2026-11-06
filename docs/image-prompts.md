# 치과 키우기 이미지 프롬프트

받은 파일은 아래 **파일 이름** 그대로 `assets/images/` 에 넣는다. 코드는 이 이름으로 읽는다.

## 쓰는 법

- 모든 프롬프트 앞에 **공통 스타일**을 붙인다. 그림체가 어긋나지 않게 첫 장을 고른 뒤, 그 그림을 참고 이미지로 함께 넣는다.
- `[투명]` 표시는 뒤에 **투명 스타일**도 붙인다. 투명 PNG가 안 되는 도구면 그대로 받아 오면 된다. 단색 배경은 코드 쪽 도구로 지운다.
- 글자는 그림에 넣지 않는다. 한글은 AI가 깨뜨리기 쉬워서 글자는 전부 게임에서 그린다.
- 우선순위: **1차** 먼저 받을 그림 / **2차** 나중에 받을 그림. 없는 그림은 임시 도형으로 나오므로 게임은 그대로 돌아간다.

### 공통 스타일

```
Modern Korean webtoon illustration style: clean confident ink line art, soft flat cel coloring with gentle airbrushed gradients, pale warm skin tones with light blush on the cheeks, realistic adult proportions and natural Korean faces, delicate hair strands, muted warm neutral palette of white, light gray and warm wood, the only saturated blue is where specifically requested, bright soft lighting, set in a present-day South Korean dental clinic (not an American or European dental office) unless stated otherwise, with Korean staff and patients. No text, no letters, no speech bubbles, no sound effects, no panels, no logos, no watermark.
```

### 투명 스타일

```
Single isolated object centered, full subject visible with margin around it, transparent background. If transparency is unavailable, use a flat solid magenta #FF00FF background with no shadow on the background.
```

## 1. 공간 배경 (세로 1080×1920, 불투명) — 1차

체어를 코드가 위에 올리므로 **가운데 바닥은 비워 둔다.**

공통 뒤에 붙일 문구:

```
Vertical 9:16 interior background, top-down 3/4 view, the center and lower floor area kept completely empty with no furniture so game objects can be placed on it, details only along the top wall and edges. Typical Korean clinic interior: bright white rectangular LED ceiling panels, white walls, light wood-look vinyl tile floor, low frosted glass partitions, a water purifier, a wall-mounted TV, a slipper rack and a long fabric waiting sofa, all signs blank. The accent wall panels and trim are painted in flat pure cobalt blue #0047FF, and nothing else in the room is blue; any sky through windows is pale and hazy. After hours: the clinic is completely deserted, absolutely no people, no patients, no staff, no human figures anywhere.
```

꾸미기에서 고른 색으로 코발트 블루 부분이 바뀐다. 파랗게 나와도 맞게 나온 것이다.

| 파일 이름 | 프롬프트 |
|---|---|
| `bg_space1.png` | A tiny second-floor clinic in an aging Korean sangga shopping building, one small waiting sofa, a window with frosted film, a narrow reception counter, modest and tidy |
| `bg_space2.png` | The same small neighborhood clinic after expansion, wider room, fresh wallpaper, a small reception corner, potted plant |
| `bg_space3.png` | One floor of a modern medical building, clean white walls, large windows with a city view, neat reception desk along the wall |
| `bg_space4.png` | A larger upscale floor of a medical building, glass partitions, wood accents, comfortable waiting lounge along the wall |
| `bg_space5.png` | A spacious dental hospital, bright lobby feel, high ceiling, tall windows, indoor plants, polished floor |
| `bg_space6.png` | The grand lobby-like interior of a dedicated dental tower, luxurious marble floor, gold details, panoramic skyline windows |

## 2. 유닛체어와 진료 장면 (512×512) [투명] — 1차

진료 그림은 의사가 남 · 여로 나뉜다: `chair_busy_m/f.png`, `chair_busy_hyg_m/f.png`. 의사가 없는 체어는 `chair_wait.png`(환자 대기), `chair_hyg_only.png`(위생사 단독 스케일링). `chair_idle.png` 을 첨부해 같은 체어로 만든다. 같은 각도, 같은 체어로 나와야 한다. 첫 장을 참고 이미지로 쓴다. 시트의 코발트 블루는 꾸미기에서 고른 색으로 바뀐다.

| 파일 이름 | 프롬프트 |
|---|---|
| `chair_idle.png` | A compact integrated dental unit chair of the kind used in Korean clinics, with an attached spittoon bowl, cup filler and instrument tray arm, seen from top-down 3/4 view, facing lower left, upholstery in pure cobalt blue #0047FF, white frame, overhead lamp arm, small side tray, empty and unoccupied |
| `chair_busy.png` | The same dental unit chair with the same cobalt blue upholstery, same angle, no other blue objects, an adult patient lying back in it, a dentist in white coat and mask leaning in with a mirror, lamp lit |
| `chair_busy_hyg.png` | The same dental unit chair with the same cobalt blue upholstery, same angle, no other blue objects, patient lying back, dentist working, and a dental hygienist in mint scrubs assisting beside them |

## 3. 탭 대상 (768×768) [투명] — 1차

화면에서 가장 많이 누르는 그림이다.

| 파일 이름 | 프롬프트 |
|---|---|
| `tap_model.png` | A dental student training phantom head (토마스): a smooth bald off-white plastic mannequin skull, with a terracotta skin-toned rubber face mask covering the lower face from the eyes down, a simple sculpted nose, and one large open triangular mouth hole showing teeth inside, no eyes, no hair, mounted on the headrest of a dental practice chair, front view |
| `tap_mouth.png` | Close-up of a patient open mouth during a dental check-up, healthy upper and lower teeth, pink gums, seen straight on, cropped to the lips, front view |
| `tap_tooth.png` | A single large healthy human molar tooth, realistic glossy enamel, clean and bright, with a soft highlight, no face |

## 4. 인물 초상 (512×512, 가슴 위) [투명]

주인공은 시작할 때 남 · 여 중에서 고른다. 그래서 주인공 그림은 **남(`_m`) · 여(`_f`) 두 벌**이다. 벌마다 첫 장을 참고 이미지로 써서 같은 얼굴로 맞춘다.

| 파일 이름 | 우선 | 프롬프트 |
|---|---|---|
| `face_director_m.png` | 1차 | Semi-realistic bust portrait of a Korean man in his 30s, a proud clinic director in a tailored white coat with a name badge, warm smile |
| `face_director_f.png` | 1차 | Semi-realistic bust portrait of a Korean woman in her 30s, a proud clinic director in a tailored white coat with a name badge, warm smile |
| `face_student_m.png` | 1차 | The same man, younger, a dental student in a short white practice gown, earnest expression, holding a notebook |
| `face_student_f.png` | 1차 | The same woman, younger, a dental student in a short white practice gown, earnest expression, holding a notebook |
| `face_paydoctor_m.png` | 1차 | The same man as a confident employed dentist in a clean white coat |
| `face_paydoctor_f.png` | 1차 | The same woman as a confident employed dentist in a clean white coat |
| `face_resident_m.png` | 2차 | The same man as a tired but determined dental resident in green scrubs, slight dark circles under the eyes |
| `face_resident_f.png` | 2차 | The same woman as a tired but determined dental resident in green scrubs, slight dark circles under the eyes |
| `face_dentist.png` | 1차 | Semi-realistic bust portrait of a Korean hired dentist in their 30s in white coat with a surgical mask pulled under the chin |
| `face_hygienist.png` | 1차 | Semi-realistic bust portrait of a Korean dental hygienist in mint scrubs with a face shield pushed up |
| `face_desk.png` | 1차 | Semi-realistic bust portrait of a Korean front desk coordinator in a navy vest with a headset, polite smile |
| `face_manager.png` | 2차 | Semi-realistic bust portrait of a Korean clinic office manager in their 40s in a blazer holding a tablet |
| `face_ortho.png` | 2차 | Semi-realistic bust portrait of a Korean orthodontist in white coat holding a small model with braces |

대사에 나오는 인물:

| 파일 이름 | 우선 | 프롬프트 |
|---|---|---|
| `face_mother.png` | 1차 | Semi-realistic bust portrait of a warm, tired Korean mother in her 50s in a plain cardigan, gentle worried smile |
| `face_old_dentist.png` | 1차 | Semi-realistic bust portrait of a kind Korean dentist in their 50s in an old-fashioned white gown, glasses, patient gentle expression |
| `face_senior.png` | 1차 | Semi-realistic bust portrait of a Korean senior dental student in their late 20s in a white gown, confident friendly smirk |
| `face_professor.png` | 1차 | Semi-realistic bust portrait of a strict Korean dental professor in their 60s in a white coat, stern but fair expression |

## 5. 진료 메뉴 아이콘 (256×256) [투명] — 1차

공통 뒤에 붙일 문구: `Game icon, bold simple silhouette readable at small size.`

| 파일 이름 | 프롬프트 |
|---|---|
| `tx_scaling.png` | A dental ultrasonic scaler tip with sparkling clean tooth |
| `tx_resin.png` | A tooth with a small glowing blue curing light filling a cavity |
| `tx_root.png` | A cross-section of a tooth showing root canals with a thin file |
| `tx_crown.png` | A shiny golden-white dental crown hovering above a tooth stump |
| `tx_implant.png` | A dental implant with a screw post and a white crown on top |
| `tx_ortho.png` | A smiling row of teeth with metal braces and a wire |
| `tx_fullmouth.png` | A full perfect set of glowing teeth on a pedestal with radiant light |

## 6. 장비 · 업그레이드 아이콘 (256×256) [투명]

공통 뒤에 붙일 문구: `Game icon, bold simple silhouette readable at small size.`

| 파일 이름 | 우선 | 프롬프트 |
|---|---|---|
| `eq_scanner.png` | 1차 | A handheld intraoral 3D scanner wand with a small blue scan beam |
| `eq_ct.png` | 1차 | A dental cone-beam CT machine, standing unit with a rotating arm |
| `up_skill.png` | 1차 | A dental handpiece drill with motion speed lines and a small star |
| `up_loupe.png` | 1차 | Dental surgical loupes, glasses with small magnifying lenses |
| `up_model.png` | 1차 | A set of small practice model teeth in a tray |
| `up_notes.png` | 1차 | A worn senior student's notebook with sticky tabs |
| `up_allnight.png` | 2차 | A desk lamp and a coffee cup beside a tooth model under a night moon |
| `up_incentive.png` | 2차 | A handshake with a coin above it |
| `up_nightshift.png` | 2차 | A clinic door with a lit sign shape and a crescent moon, no writing |
| `up_seminar.png` | 2차 | A small lecture podium with a graduation-style cap and a tooth |

## 7. 재화 · 상태 아이콘 (128×128) [투명] — 1차

공통 뒤에 붙일 문구: `Tiny game UI icon, extremely simple, readable at 48 pixels.`

| 파일 이름 | 프롬프트 |
|---|---|
| `ic_money.png` | A realistic gold coin with a small tooth emblem |
| `ic_xp.png` | A glowing mint star |
| `ic_reputation.png` | A heart with a small check mark shape |
| `ic_fame.png` | A golden laurel crown |
| `ic_patient.png` | A simple round person silhouette |
| `ic_warn.png` | A round orange warning badge with an exclamation shape |

## 8. 이벤트 (768×768) [투명] — 2차

| 파일 이름 | 프롬프트 |
|---|---|
| `ev_wreath.png` | A big congratulatory Korean opening flower wreath on a stand, blank ribbons with no writing |
| `ev_emergency.png` | A worried adult patient holding a swollen cheek |
| `ev_complaint.png` | An irritated adult patient at a clinic counter, arms crossed, frowning |
| `ev_review.png` | A smartphone showing five golden stars floating out of the screen, no writing |
| `ev_broken.png` | A dental chair with an open service panel and a wrench beside it |

## 9. 앱 표지 — 1차

| 파일 이름 | 크기 | 프롬프트 |
|---|---|---|
| `app_icon.png` | 1024×1024, 불투명 | App icon, a single realistic glossy white molar tooth with a dental mirror beside it, centered, soft teal gradient background, fills the frame, no text |
| `title_bg.png` | 1080×1920, 불투명 | Vertical 9:16 title screen, a small neighborhood dental clinic in a Korean shopping building in the foreground with a tall modern dental tower rising behind it at golden hour, open sky in the upper third left empty for a logo, no text |

## 10. 스토리 (정사각, 불투명) — 2차

대사 장면 뒤에 깔린다. `tools/gemini_batch.js` 대기열에 들어 있다.

| 파일 이름 | 프롬프트 |
|---|---|
| `story_childhood.png` | A crying seven-year-old Korean child holding their mother's hand in front of a small 1990s Korean neighborhood dental clinic, warm nostalgic evening light, seen from behind at a slight angle |
| `story_study.png` | A Korean high school student asleep over open workbooks in a dim study room late at night, a desk lamp glowing, a lunch box left by the mother beside them, seen from behind |
| `story_student.png` | A Korean dental student seen from behind putting on a white gown in a hospital corridor on their first day, morning light |
| `story_kind_dentist.png` | A gentle Korean dentist kneeling to eye level beside a crying child in a modest 1990s Korean dental clinic, seen from the side, warm patient mood |
| `story_retake.png` | An empty rows-of-desks Korean cram school classroom late at night, one desk lit with piled workbooks, a lone student seen from behind |
| `story_lunchbox.png` | A mother seen from behind quietly placing a lunch box on a desk beside a sleeping student in a dim room, warm lamp light |
| `story_letter.png` | A Korean mother seen from behind wiping her eyes at a small kitchen sink, a phone on the table glowing, warm morning light, no writing |
| `story_phantom.png` | A Korean dental school simulation lab with rows of practice dental chairs holding phantom heads, fluorescent lights, students in white gowns seen from behind |
| `face_student_m_loupe.png`, `face_student_f_loupe.png` | 원본 초상을 첨부해서: keep the same person and add dental surgical loupes worn on the eyes |
| `story_clinic.png` | The lit sign of a small second-floor dental clinic in a Korean shopping building at night, seen from the street, blank sign with no writing, warm hopeful mood |
