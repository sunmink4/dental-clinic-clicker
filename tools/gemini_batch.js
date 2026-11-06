// 로그인된 Gemini 창에서 그림을 차례로 만들어 art/raw 에 받는다. 이미 있는 파일은 건너뛰므로 다시 돌리면 이어서 한다.
// Playwright 브라우저 도구의 run_code 로 이 파일을 실행한다. 한 번에 LIMIT 장.
async (page) => {
  const LIMIT = 12;
  const ONLY = /^story_/;
  const EXISTING = [];
  const OUT = 'C:/Users/ocean/git/clicker/dental-clinic-clicker/art/raw/';

  const STYLE = 'Modern Korean webtoon illustration style: clean confident ink line art, soft flat cel coloring with gentle airbrushed gradients, pale warm skin tones with light blush on the cheeks, realistic adult proportions and natural Korean faces, delicate hair strands, muted warm neutral palette of white, light gray and warm wood, the only saturated blue is where specifically requested, bright soft lighting, set in a present-day South Korean dental clinic (not an American or European dental office) unless stated otherwise, with Korean staff and patients. No text, no letters, no speech bubbles, no sound effects, no panels, no logos, no watermark. Full-bleed image with no border or frame; no posters, calendars, charts or signs with any writing or symbols.';
  const CUT = 'Single isolated subject centered, full subject visible with margin around it, on a flat solid magenta #FF00FF background with no shadow on the background.';
  const ROOM = 'Wide horizontal 3:2 interior of a single dental treatment room seen from above at a top-down 3/4 view. The whole floor is a large empty open space with no furniture, no chairs, no dividing walls and no partitions, so game objects can be placed on it. Details only along the top wall: cabinets, a sink and windows. Typical Korean clinic interior: bright white rectangular LED ceiling panels, white walls, light wood-look vinyl tile floor, all signs blank. The accent wall panels and trim are painted in flat pure cobalt blue #0047FF, and nothing else in the room is blue; any sky through windows is pale and hazy. After hours: completely deserted, absolutely no people, no human figures anywhere.';
  const LOUNGE = 'Very wide horizontal banner interior of a dental clinic waiting room seen straight on from the front, low camera. A long fabric waiting sofa fills the center and right, a reception counter stands at the far left, nothing in the foreground. Typical Korean clinic interior: bright white rectangular LED ceiling panels, white walls, light wood-look vinyl tile floor, all signs blank. The accent wall panels and trim are painted in flat pure cobalt blue #0047FF, and nothing else in the room is blue; any sky through windows is pale and hazy. After hours: completely deserted, absolutely no people, no human figures anywhere.';
  const ICON = 'Game icon, bold simple silhouette readable at small size. Square 1:1.';
  const TINY = 'Tiny game UI icon, extremely simple, readable at 48 pixels. Square 1:1.';
  const SAME = 'Keep the exact same person, face, hair, art style and flat magenta #FF00FF background. Generate a new square image:';

  const lounge = (name, text) => [name, `Generate an image. Wide 16:9. ${STYLE} ${LOUNGE} ${text}`];
  const room = (name, text) => [name, `Generate an image. ${STYLE} ${ROOM} ${text}`];
  const cut = (name, text, extra = 'Square 1:1.') => [name, `Generate an image. ${extra} ${STYLE} ${text} ${CUT}`];
  const same = (name, text) => [name, `${SAME} ${text}`, true];
  const story = (name, text) => [name, `Generate an image. Square 1:1. ${STYLE} A single full-bleed story scene. ${text}`];

  const PHANTOM = 'A dental student training phantom head: a smooth bald off-white plastic mannequin skull, with a terracotta skin-toned rubber face mask covering the lower face from the eyes down, a simple sculpted nose, and one large open triangular mouth hole showing teeth inside, no eyes, no hair.';

  const hero = (name, who) => cut(name, `Bust portrait of the game's main character, ${who}, a Korean dentist in a white coat over navy scrubs, friendly confident expression, facing slightly left, same framing from the chest up.`);
  const HEROES = [
    hero('hero_m1', 'a man in his late 20s with short neat black hair'),
    hero('hero_m2', 'a man in his late 20s with a soft side-parted fringe and thin glasses'),
    hero('hero_m3', 'a man in his 30s with a slicked-back black undercut and sharp eyes, no square, no outline around the portrait'),
    hero('hero_m4', 'a man in his 30s with wavy medium-length hair and a gentle smile'),
    hero('hero_m5', 'a man in his late 20s with a buzz cut and an athletic build'),
    hero('hero_m6', 'a man in his 30s with round glasses and a center part'),
    hero('hero_m7', 'a man in his 40s with short hair, slight gray at the temples'),
    hero('hero_m8', 'a chubby cheerful man in his 30s with short curly hair'),
    hero('hero_m9', 'a man in his late 20s with long bangs and a calm, quiet look'),
    hero('hero_f1', 'a woman in her late 20s with a low ponytail and soft bangs'),
    hero('hero_f2', 'a woman in her late 20s with a short bob cut'),
    hero('hero_f3', 'a woman in her 30s with long straight black hair tied half up'),
    hero('hero_f4', 'a woman in her 30s with wavy shoulder-length brown hair'),
    hero('hero_f5', 'a woman in her late 20s with a pixie cut and a bright smile'),
    hero('hero_f6', 'a woman in her 30s with thin glasses and a neat low bun'),
    hero('hero_f7', 'a woman in her 40s with short layered hair and a warm mature look'),
    hero('hero_f8', 'a round-faced cheerful woman in her 30s with a high ponytail'),
    hero('hero_f9', 'a woman in her late 20s with long bangs and a calm, quiet look'),
  ];

  const HYGIENISTS = [
    ['a woman in her early 20s with a neat low ponytail'],
    ['a woman in her 20s with a short bob and soft bangs'],
    ['a woman in her 20s with a high ponytail and a bright smile'],
    ['a woman in her 30s with a tidy low bun'],
    ['a woman in her 30s with thin glasses and shoulder-length hair tied back'],
    ['a woman in her 40s with short permed hair and a warm smile'],
    ['a woman in her 20s with a side braid'],
    ['a woman in her 30s with a half-up hairstyle'],
    ['a woman in her 20s with round glasses and a short ponytail'],
    ['a woman in her 50s with short neat hair, an experienced head hygienist'],
    ['a woman in her 20s with a pixie cut'],
    ['a woman in her 30s with a long ponytail and a calm look'],
    ['a woman in her 20s with twin low buns and a shy smile'],
    ['a woman in her 30s with wavy hair tied at the nape'],
    ['a woman in her 40s with glasses on a chain and a low bun'],
    ['a man in his 20s with short neat hair'],
    ['a man in his 30s with glasses and a gentle smile'],
    ['a woman in her early 20s with a surgical cap, nervous new-graduate expression'],
    ['a woman in her 30s with a confident grin and hair clipped up'],
    ['a woman in her 20s with long bangs and a quiet look'],
  ].map(([who], i) => cut(`hyg_${i + 1}`, `Bust portrait of a Korean dental hygienist, ${who}, in mint scrubs, friendly professional expression, facing slightly left, same framing from the chest up, no square or outline around the portrait.`));

  const GEAR = [
    ['xray', 'A floor-standing dental panoramic X-ray machine with a rotating arm.'],
    ['autoclave', 'A compact stainless steel dental autoclave sterilizer on a small white cabinet.'],
    ['compressor', 'A small quiet dental air compressor unit with a tank.'],
    ['printer', 'A compact dental 3D printer with a clear orange lid.'],
  ].map(([id, what]) => cut(`eq_${id}`, `${what} Seen from a top-down 3/4 view, as equipment for a management game, no writing.`));

  // 마스크를 장착하면 쓰는 얼굴. 원본 얼굴을 첨부해 같은 사람으로 만든다.
  const MASKED = HEROES.map(([name]) => [`${name}_mask`, 'Edit the attached portrait. Keep the exact same person, face, hair, clothes, pose, framing, art style and flat magenta #FF00FF background. Only add a plain white surgical face mask worn over the nose and mouth, with ear loops.', false, name]);

  // 체어에 눕는 환자. 원본 체어 그림을 첨부해 환자만 바꾼다.
  const PATIENTS = [
    ['kid', 'a Korean child about eight years old'],
    ['woman', 'a young Korean woman in her 20s with long hair'],
    ['gran', 'an elderly Korean grandmother in her 70s with short gray permed hair'],
    ['grandpa', 'an elderly Korean grandfather in his 70s with thin gray hair'],
  ].flatMap(([id, who]) => ['chair_wait', 'chair_hyg_only', 'chair_busy_m', 'chair_busy_f', 'chair_busy_hyg_m', 'chair_busy_hyg_f'].map(base =>
    [`${base}_${id}`, `Edit the attached image. Keep everything exactly the same: the chair, its color, the angle, the lamp, every staff member, the art style and the flat magenta #FF00FF background. Only replace the patient lying in the chair with ${who}, lying in the same position.`, false, base]));

  const QUEUE = [
    ...MASKED,
    ...PATIENTS,
    cut('chair_idle', 'A compact integrated dental unit chair of the kind used in Korean clinics, with an attached spittoon bowl, cup filler and instrument tray arm, seen from top-down 3/4 view, facing lower left, upholstery in flat pure cobalt blue #0047FF, white frame, overhead lamp arm, small side tray, empty and unoccupied, no light beam from the lamp.'),
    ...GEAR,
    ...HYGIENISTS,
    ...HEROES,
    cut('eq_move', 'A moving cardboard box with a small dental tooth model on top and a key beside it, no writing.', ICON),
    cut('mn_profile', 'A simple dentist ID badge with a blank portrait silhouette and no writing.', ICON),
    cut('mn_decor', 'A paint roller and three small paint color swatches.', ICON),
    cut('mn_music', 'A single solid dark teal eighth music note beside small dark gray headphones, strong dark colors, nothing white.', ICON),
    cut('mn_slots', 'Three small stacked save folders, no writing.', ICON),
    cut('mn_new', 'A fresh white tooth with a small green sprout growing from it.', ICON),
    cut('mn_wiki', 'An open thick reference book with a small tooth bookmark, blank pages with no writing.', ICON),
    cut('mn_close', 'A simple open door with an arrow shape pointing out, no writing.', ICON),
    cut('it_faceshield', 'A clear dental face shield visor on a white headband.', ICON),
    cut('it_glasses', 'A pair of clear protective safety glasses.', ICON),
    cut('it_mask', 'A white KF94 style folded face mask with ear loops, no writing.', ICON),
    cut('it_gown', 'A clean white doctor coat on a hanger.', ICON),
    cut('it_scrubs', 'A folded set of mint dental scrubs, top and pants.', ICON),
    cut('it_handpiece', 'A single stainless steel dental handpiece, nothing else.', ICON),
    cut('it_chair', 'A wheeled dentist operator stool with a round padded seat and a backrest.', ICON),
    cut('sk_diagnosis','A dental X-ray film with a magnifying glass over one tooth.', ICON),
    cut('sk_technique', 'A dental handpiece drill held by a gloved hand, with a small star.', ICON),
    cut('sk_care', 'A gloved hand gently holding a small heart with a tooth shape inside.', ICON),
    cut('sk_consult', 'One white speech bubble and one light gray speech bubble overlapping, with a small white tooth model in front of them. Only these objects, no desk, no room, no scene behind them, no writing.', ICON),
    cut('sk_leadership', 'Three simple staff figures in scrubs standing together with a small star above them.', ICON),
    cut('sk_stamina', 'A white coffee cup with gray steam beside a battery, the battery body completely filled with solid bright green, no transparent or see-through parts, no writing.', ICON),
    [ 'chair_wait', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. Add only an adult Korean patient lying back in the chair, waiting calmly. No dentist and no staff.`, false, 'chair_idle' ],
    [ 'chair_hyg_only', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. The lamp, its arm, the spittoon and the instrument tray must stay unchanged, in the same place and color. Add an adult Korean patient lying flat on the blue seat, head on the headrest at the right end and feet at the left end, and a Korean dental hygienist in mint scrubs and a mask working alone doing scaling. No dentist. Only these two people.`, false, 'chair_idle' ],
    [ 'chair_busy_m', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. The lamp, its arm, the spittoon and the instrument tray must stay unchanged, in the same place and color. Add an adult Korean patient lying flat on the blue seat, head on the headrest at the right end and feet at the left end, and a Korean man as the dentist in a white coat and surgical mask, leaning in with a dental mirror. Only these two people.`, false, 'chair_idle' ],
    [ 'chair_busy_hyg_m', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. The lamp, its arm, the spittoon and the instrument tray must stay unchanged, in the same place and color. Add an adult Korean patient lying flat on the blue seat, head on the headrest at the right end and feet at the left end, and a Korean man as the dentist in a white coat and surgical mask on one side, and a Korean dental hygienist in mint scrubs assisting on the other side. Only these three people, all clearly visible.`, false, 'chair_idle' ],
    [ 'chair_busy_f', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. The lamp, its arm, the spittoon and the instrument tray must stay unchanged, in the same place and color. Add an adult Korean patient lying flat on the blue seat, head on the headrest at the right end and feet at the left end, and a Korean woman with her hair tied back as the dentist in a white coat and surgical mask, leaning in with a dental mirror. Only these two people.`, false, 'chair_idle' ],
    [ 'chair_busy_hyg_f', `Edit the attached dental chair image. Keep the exact same chair, angle, cobalt blue upholstery, art style and flat magenta #FF00FF background. The lamp has no visible light beam or glow. No other blue objects. The lamp, its arm, the spittoon and the instrument tray must stay unchanged, in the same place and color. Add an adult Korean patient lying flat on the blue seat, head on the headrest at the right end and feet at the left end, and a Korean woman with her hair tied back as the dentist in a white coat and surgical mask on one side, and a Korean dental hygienist in mint scrubs assisting on the other side. Only these three people, all clearly visible.`, false, 'chair_idle' ],
    cut('tap_mouth', 'Close-up of a patient open mouth during a dental check-up, healthy upper and lower teeth, pink gums, seen straight on, cropped to the lips, front view.'),
    [ 'face_student_f_loupe', `Edit the attached portrait. Keep the exact same person, face, hair, clothes, pose, art style and flat magenta #FF00FF background. Only add dental surgical loupes worn on the eyes: dark eyeglass frames with two small cylindrical magnifying telescopes through the lenses.`, false, 'face_student_f' ],
    [ 'face_student_m_loupe', `Edit the attached portrait. Keep the exact same person, face, hair, clothes, pose, art style and flat magenta #FF00FF background. Only add dental surgical loupes worn on the eyes: dark eyeglass frames with two small cylindrical magnifying telescopes through the lenses.`, false, 'face_student_m' ],
    story('story_kind_dentist', 'A gentle Korean dentist kneeling to eye level beside a crying child in a modest 1990s Korean dental clinic, seen from the side, warm patient mood.'),
    story('story_retake', 'An empty rows-of-desks Korean cram school classroom late at night, one desk lit with piled workbooks, a lone student seen from behind.'),
    story('story_lunchbox', 'A mother seen from behind quietly placing a lunch box on a desk beside a sleeping student in a dim room, warm lamp light.'),
    story('story_letter', 'A Korean mother seen from behind wiping her eyes at a small kitchen sink, a phone on the table glowing, warm morning light, no writing.'),
    story('story_phantom', `A Korean dental school simulation lab with rows of practice dental chairs, each holding this training head: ${PHANTOM} Fluorescent lights, students in white gowns seen from behind.`),
    cut('face_mother', 'Bust portrait of a warm, tired Korean mother in her 50s in a plain cardigan, gentle worried smile.'),
    cut('face_old_dentist', 'Bust portrait of a kind Korean woman dentist in her 40s, long dark hair in a low ponytail, white coat over light blue scrubs, a surgical mask pulled under her chin, warm gentle smile.'),
    cut('face_senior', 'Bust portrait of a Korean senior dental student in their late 20s in a white gown, confident friendly smirk.'),
    cut('face_professor', 'Bust portrait of a strict Korean dental professor in their 60s in a white coat, stern but fair expression.'),
    cut('tap_tooth', 'A single large healthy human molar tooth, realistic glossy pure white enamel, clean and bright, with a soft highlight, no face, nothing blue.'),
    story('story_childhood', 'A crying seven-year-old Korean child holding their mother\'s hand in front of a small 1990s Korean neighborhood dental clinic, warm nostalgic evening light, seen from behind at a slight angle.'),
    story('story_study', 'A Korean high school student asleep over open workbooks in a dim study room late at night, a desk lamp glowing, a lunch box left by the mother beside them, seen from behind.'),
    story('story_student', 'A Korean dental student seen from behind putting on a white gown in a hospital corridor on their first day, morning light.'),
    story('story_clinic', 'The lit sign of a small second-floor dental clinic in a Korean shopping building at night, seen from the street, blank sign with no writing, warm hopeful mood.'),
    // 장면마다 따로 그리는 웹툰 컷. 주인공은 남녀 둘 다라서 뒷모습 · 손 · 먼 거리로만 보인다.
    story('story_anatomy', 'A Korean dental school library late at night, a desk buried in thick anatomy textbooks, tooth morphology carving wax blocks and a skull model, a student seen from behind with head in hands.'),
    story('story_result_call', 'Close-up of a young adult hand holding a smartphone to the ear at a bright window, happy tears glinting, the screen not visible, warm joyful morning light, seen from behind over the shoulder.'),
    story('story_crossroads', 'A young doctor in a white coat seen from behind standing at a hospital corridor crossroads, one way leading to a bright university hospital ward, the other to a sunny street of small clinics.'),
    story('story_resident', 'A Korean university dental hospital at dawn, a tired resident in green scrubs seen from behind reviewing stacks of patient charts under a desk lamp, a cold coffee cup beside them.'),
    story('story_scold', 'A stern Korean dental professor in a white coat pointing at an X-ray on a light box, scolding a resident in green scrubs who is seen from behind with head bowed, serious mood.'),
    story('story_paycheck', 'Close-up of hands holding a smartphone showing a bank transfer screen with only shapes and no readable writing, sending money, a small studio apartment at night, warm lamp light.'),
    story('story_dream_clinic', 'An empty second-floor shop unit for rent in an aging Korean shopping building, bare concrete and big windows, a young doctor seen from behind standing in the middle imagining a clinic, faint dreamy outlines of a dental chair.'),
    story('story_opening_small', 'A tiny brand-new Korean neighborhood dental clinic with a single dental chair, fresh wallpaper, a small reception desk, congratulatory flower pots by the door, opening morning, no people.'),
    story('story_opening_mother', 'A Korean mother in her 50s in a plain cardigan smiling with teary eyes, looking up at a lit clinic sign at night, the sign blank, her grown child seen from behind beside her.'),
    story('story_waiting_full', 'A small Korean dental clinic waiting room packed with patients of all ages on every chair, some standing, a busy reception desk, cheerful bustling mood.'),
    story('story_medical_building', 'Elevator doors opening onto a bright modern dental clinic lobby on one floor of a medical building, a wide reception counter and glass walls, seen from inside the elevator.'),
    story('story_meeting', 'A Korean clinic office manager in a blazer showing a tablet with a map pin graphic and no writing to a doctor seen from behind, in a small office after hours, hopeful mood.'),
    story('story_branch_open', 'The opening ceremony of a second Korean dental clinic in another neighborhood, staff in scrubs lined up, a red ribbon being cut, flower wreaths with blank ribbons, a doctor seen from behind holding scissors.'),
    story('story_hospital_ceremony', 'A grand Korean dental hospital opening ceremony in a lobby, rows of chairs, a proud Korean mother in her 60s sitting in the front row clapping with teary eyes, the doctor on the stage seen small from behind.'),
    story('story_tower_night', 'A tall modern dental tower building with all windows lit at night in a Korean city, the rooftop sign blank, seen from the street below, hopeful mood.'),
    story('story_tower_child', 'A bright pediatric dental room in a modern hospital, a doctor in a white coat seen from behind kneeling to eye level with a crying child holding the hand of their mother, warm gentle mood, echoing an old memory.'),
    cut('tap_model', PHANTOM + ' Mounted on the headrest of a dental practice chair, front view.'),
    cut('face_director_m', 'Bust portrait of a Korean man in his 30s, a proud clinic director in a tailored white coat with a blank name badge, warm smile.'),
    same('face_student_m', 'the same man, younger, a dental student in a short white practice gown, earnest expression, holding a notebook.'),
    same('face_paydoctor_m', 'the same man as a confident employed dentist in a clean white coat.'),
    same('face_resident_m', 'the same man as a tired but determined dental resident in green scrubs, slight dark circles under the eyes.'),
    cut('face_director_f', 'Bust portrait of a Korean woman in her 30s, a proud clinic director in a tailored white coat with a blank name badge, warm smile.'),
    same('face_student_f', 'the same woman, younger, a dental student in a short white practice gown, earnest expression, holding a notebook.'),
    same('face_paydoctor_f', 'the same woman as a confident employed dentist in a clean white coat.'),
    same('face_resident_f', 'the same woman as a tired but determined dental resident in green scrubs, slight dark circles under the eyes.'),
    cut('face_dentist', 'Bust portrait of a Korean hired dentist in their 30s in white coat with a surgical mask pulled under the chin.'),
    cut('face_dentist_f', 'Bust portrait of a Korean woman hired dentist in her 30s with her hair tied back, in a white coat over navy scrubs with a blue surgical mask pulled under the chin, a stethoscope and a blank name badge, gentle confident smile, facing forward.'),
    cut('face_hygienist', 'Bust portrait of a Korean dental hygienist in mint scrubs with a face shield pushed up.'),
    cut('face_desk', 'Bust portrait of a Korean front desk coordinator in a navy vest with a headset, polite smile.'),
    lounge('wait_space1', 'A cramped waiting corner in an aging Korean shopping building, a small sofa, frosted film window.'),
    lounge('wait_space2', 'A tidy small waiting room with fresh wallpaper and a potted plant.'),
    lounge('wait_space3', 'A clean modern waiting room with a large window and a water purifier.'),
    lounge('wait_space4', 'An upscale waiting lounge with wood accents and glass partitions.'),
    lounge('wait_space5', 'A bright hospital lobby waiting area with indoor plants.'),
    lounge('wait_space6', 'A grand marble lobby waiting lounge with gold details and a skyline window.'),
    room('bg_space1', 'A tiny second-floor clinic in an aging Korean sangga shopping building, a window with frosted film, modest and tidy.'),
    room('bg_space2', 'A small neighborhood clinic after expansion, wider room, fresh wallpaper.'),
    room('bg_space3', 'One floor of a modern medical building, clean white walls, large windows with a city view.'),
    room('bg_space4', 'A larger upscale floor of a medical building, wood accents along the top wall.'),
    room('bg_space5', 'A spacious dental hospital treatment floor, high ceiling, tall windows, polished floor.'),
    room('bg_space6', 'A luxurious treatment floor in a dedicated dental tower, marble floor, gold details, panoramic skyline windows.'),
    cut('tx_scaling', 'A dental ultrasonic scaler tip with sparkling clean tooth.', ICON),
    cut('tx_resin', 'A tooth with a small glowing blue curing light filling a cavity.', ICON),
    cut('tx_root', 'A cross-section of a tooth showing root canals with a thin file.', ICON),
    cut('tx_fluoride', 'A small brush applying clear fluoride gel to a child tooth.', ICON),
    cut('tx_sealant', 'A molar tooth with a thin clear protective coating on its chewing surface.', ICON),
    cut('tx_extraction', 'Dental forceps holding an extracted tooth.', ICON),
    cut('tx_wisdom', 'A sideways impacted wisdom tooth in a jaw cross-section.', ICON),
    cut('tx_inlay', 'A molar with a filling piece fitted inside its top surface.', ICON),
    cut('tx_onlay', 'A molar with a restoration covering one cusp.', ICON),
    cut('tx_pedo_resin', 'A small baby tooth being filled with a tooth-colored resin.', ICON),
    cut('tx_pedo_crown', 'A small shiny stainless steel crown on a baby molar.', ICON),
    cut('tx_crown', 'A shiny golden-white dental crown hovering above a tooth stump.', ICON),
    cut('tx_implant', 'A dental implant with a screw post and a white crown on top.', ICON),
    cut('tx_ortho', 'A smiling row of teeth with metal braces and a wire.', ICON),
    cut('tx_fullmouth', 'A full perfect set of glowing teeth on a pedestal with radiant light.', ICON),
    cut('eq_scanner', 'A handheld intraoral 3D scanner wand with a small blue scan beam.', ICON),
    cut('eq_ct', 'A dental cone-beam CT machine, standing unit with a rotating arm.', ICON),
    cut('up_skill', 'A dental handpiece drill with motion speed lines and a small star.', ICON),
    cut('up_loupe', 'Dental surgical loupes, glasses with small magnifying lenses.', ICON),
    cut('up_model', 'A set of small practice model teeth in a tray.', ICON),
    cut('up_notes', "A worn senior student's notebook with sticky tabs.", ICON),
    cut('ic_money', 'A realistic gold coin with a small tooth emblem.', TINY),
    cut('ic_xp', 'A glowing mint star.', TINY),
    cut('ic_reputation', 'A heart with a small check mark shape.', TINY),
    cut('ic_fame', 'A golden laurel crown.', TINY),
    cut('ic_patient', 'A simple round person silhouette.', TINY),
    cut('ic_warn', 'A round orange warning badge with an exclamation shape.', TINY),
    cut('app_icon', 'App icon, a close-up head and shoulders of a friendly young Korean woman dentist with a neat low ponytail and soft bangs, wearing a plain white surgical face mask over the nose and mouth and a white coat, her eyes smiling warmly, holding up a small round dental mirror on a slim steel handle beside her face, bold and simple, large and centered, readable at small size.'),
    [ 'title_bg', `Generate an image. Vertical 9:16. ${STYLE} Title screen, a small neighborhood dental clinic in a Korean shopping building in the foreground with a tall modern dental tower rising behind it at golden hour, open sky in the upper third left empty for a logo, no text.` ],
    cut('face_manager', 'Bust portrait of a Korean clinic office manager in their 40s in a blazer holding a tablet.'),
    cut('face_ortho', 'Bust portrait of a Korean orthodontist in white coat holding a small model with braces.'),
    cut('up_allnight', 'A desk lamp and a coffee cup beside a tooth model under a night moon.', ICON),
    cut('up_incentive', 'A handshake with a coin above it.', ICON),
    cut('up_nightshift', 'A clinic door with a lit sign shape and a crescent moon, no writing.', ICON),
    cut('up_seminar', 'A small lecture podium with a graduation-style cap and a tooth.', ICON),
    cut('ev_wreath', 'A big congratulatory Korean opening flower wreath on a stand, blank ribbons with no writing.'),
    cut('ev_emergency', 'A worried adult patient holding a swollen cheek.'),
    cut('ev_complaint', 'An irritated adult patient at a clinic counter, arms crossed, frowning.'),
    cut('ev_review', 'A smartphone showing five golden stars floating out of the screen, no writing.'),
    cut('ev_broken', 'A dental chair with an open service panel and a wrench beside it.'),
  ];

  // ONLY 에 맞는 그림만 만든다. 앞쪽 접두어부터 받는다.
  const todo = QUEUE.filter(([name]) => !EXISTING.includes(name) && ONLY.test(name))
    .sort((a, b) => b[0].startsWith('mn_') - a[0].startsWith('mn_'));
  const results = [];
  let misses = 0;
  for (const [name, prompt, continues, reference] of todo.slice(0, LIMIT)) {
    if (!continues) {
      await page.goto('https://gemini.google.com/app');
      await page.getByRole('textbox', { name: 'Gemini 프롬프트 입력' }).waitFor();
      await page.waitForTimeout(3000);   // 이전 채팅 그림을 새 그림으로 착각하지 않게
      await page.waitForFunction(() => document.querySelectorAll('img.image.loaded').length === 0, null, { timeout: 15000 });
    }
    if (reference) {
      await page.getByRole('button', { name: '업로드 및 도구' }).click();
      await page.waitForTimeout(800);    // 메뉴가 열려야 숨은 입력칸이 생긴다
      await page.locator('input[type=file][accept^="image/"]').first().setInputFiles(OUT + reference + '.png');
      await page.keyboard.press('Escape');
      await page.waitForTimeout(6000);   // 첨부가 끝나기 전에 보내면 그림 없이 간다
    }
    const images = page.locator('img.image.loaded');
    const before = await images.count();
    const box = page.getByRole('textbox', { name: 'Gemini 프롬프트 입력' });
    await box.fill(prompt);
    await box.press('Enter');
    try {
      await page.waitForFunction(n => document.querySelectorAll('img.image.loaded').length > n, before, { timeout: 200000 });
    } catch {
      const said = await page.locator('message-content').last().innerText().catch(() => '');
      results.push(`${name}: 이미지 없음 — ${said.slice(0, 200)}`);
      if (++misses >= 3) break;   // 연달아 실패하면 한도로 본다
      continue;
    }
    await page.waitForTimeout(1500);
    await images.last().hover();
    const button = page.getByRole('button', { name: '원본 크기 이미지 다운로드' }).last();
    const [download] = await Promise.all([page.waitForEvent('download', { timeout: 60000 }), button.click({ force: true })]);
    await download.saveAs(OUT + name + '.png');
    misses = 0;
    results.push(`${name}: ok`);
  }
  return { done: results, left: todo.length - results.filter(r => r.endsWith('ok')).length };
}
