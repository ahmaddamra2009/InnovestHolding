# Admin guide / دليل لوحة التحكم

العربية أولاً، ثم English. / Arabic first, then English.

يصف هذا الدليل الفرع `feature/3-new-structure-brand` (المرحلة C1) قبل دمجه في `master`.
This guide describes branch `feature/3-new-structure-brand` (phase C1) before it is merged into `master`.

---

# العربية

لوحة التحكم عربية. كل نص في الموقع له قيمتان: عربية (تظهر في `/…`) وإنجليزية (تظهر في `/en/…`). الحقل العربي مطلوب
في أغلب الأحوال، والحقل الإنجليزي اختياري. بجانب كل حقل شارة «ع» أو «EN» لتعرف أي لغة تكتب.

## 1. قواعد عامة

- **كل روابط اللوحة تبدأ بـ `/Admin/`** (مثل `/Admin/Pages` و`/Admin/Events` و`/Admin/Settings`). الروابط القديمة بلا
  `/Admin` (مثل `/Dashboard` أو `/Events`) لم تعد تعمل وتعرض صفحة «غير موجود»؛ حدّثوا المفضلات. الدخول من `/Account/Login`.
- **الاحتياط (fallback):** إذا تُرك الحقل الإنجليزي فارغاً، تعرض الصفحة الإنجليزية النص العربي بدلاً منه (ويُعرض
  من اليمين إلى اليسار). الصفحة العربية لا تعرض الإنجليزي أبداً. بعض الحقول لا احتياط لها (مثل السطر العلوي في
  Hero): تبقى فارغة في الصفحة الإنجليزية إن تُرك حقلها الإنجليزي فارغاً.
- **يظهر التغيير فوراً:** بعد الحفظ يظهر في الصفحتين العربية والإنجليزية عند الطلب التالي. لا حاجة لإعادة تشغيل.
- **الإنجليزية الحالية مسودة** وتحتاج مراجعة قبل اعتمادها (ما لم يكن للنص العربي مقابل في ملف الشركة، فالإنجليزي ترجمة
  تقريبية)؛ عدّلوها من الشاشات أدناه.
- **الحقول المحايدة** (الهاتف، البريد، الروابط) لها قيمة واحدة للغتين وتُكتب من اليسار إلى اليمين.
- **الأوقات** في «التسجيلات» و«رسائل التواصل» تُحفظ بتوقيت UTC وتُعرض بتوقيت **المتصفح الخاص بمن يفتح الصفحة**
  وبلغة الصفحة. إن كان JavaScript معطّلاً تظهر القيمة بتوقيت UTC مع كلمة UTC. مرّر المؤشر على الوقت لترى قيمة UTC.
- **الروابط:** روابط التواصل الاجتماعي ورابط الجهة المطوّرة يجب أن تبدأ بـ `https://`. روابط القائمة والخدمات
  والأخبار تقبل `#قسم` أو `/مسار` أو `https://…`، وتُرفض `javascript:` وما شابهه. اتركوا الحقل فارغاً إن لم يوجد رابط.
- لا تكتبوا كلمات مرور أو بيانات سرية في أي حقل: كل ما يُكتب في المحتوى يظهر للعامة.
- **القائمة الجانبية مجمّعة:** «نظرة عامة»، «الموقع»، «منتدى إنوفست العربي»، «الأخبار»، «الوارد». اضغطوا عنوان المجموعة
  لطيّها أو فتحها؛ يتذكر المتصفح اختياركم.

## 2. بنية الموقع

الموقع الآن متعدد الصفحات. **عناوين الصفحات (الروابط) ثابتة في الكود ولا تُضاف ولا تُحذف من اللوحة**؛ نصوصها وظهورها
في القائمة وترتيبها وبيانات محركات البحث تُعدَّل من اللوحة. لكل صفحة نسخة إنجليزية بالرابط نفسه بعد `/en`.

| الصفحة | الرابط العربي | الرابط الإنجليزي |
|---|---|---|
| الرئيسية | `/` | `/en` |
| من نحن | `/about` | `/en/about` |
| منظومتنا | `/ecosystem` | `/en/ecosystem` |
| منتدى إنوفست العربي | `/forum` | `/en/forum` |
| ↳ الفعاليات | `/forum/events` | `/en/forum/events` |
| ↳ المتحدثون | `/forum/speakers` | `/en/forum/speakers` |
| ↳ الشركاء والرعاة | `/forum/partners` | `/en/forum/partners` |
| ↳ التسجيل | `/forum/registration` | `/en/forum/registration` |
| شبكة المستثمرين الملائكة | `/angel-network` | `/en/angel-network` |
| ↳ للمستثمرين | `/angel-network/investors` | `/en/angel-network/investors` |
| ↳ للشركات الناشئة | `/angel-network/startups` | `/en/angel-network/startups` |
| ↳ الفرص | `/angel-network/opportunities` | `/en/angel-network/opportunities` |
| ↳ الانضمام | `/angel-network/join` | `/en/angel-network/join` |
| التلفزيون | `/tv` | `/en/tv` |
| ↳ الحلقات | `/tv/episodes` | `/en/tv/episodes` |
| ↳ الضيوف | `/tv/guests` | `/en/tv/guests` |
| ↳ الرعاية | `/tv/sponsorship` | `/en/tv/sponsorship` |
| الأخبار | `/news` | `/en/news` |
| تواصل معنا | `/contact` | `/en/contact` |

(أسماء الصفحات في الموقع تأتي من حقل «عنوان الصفحة» وقد تختلف عن العمود الأول.) تتفرع الصفحات هكذا: الرئيسية ← من نحن،
منظومتنا، الأخبار، تواصل معنا؛ منظومتنا ← المنتدى، شبكة المستثمرين الملائكة، التلفزيون؛ ولكل منها صفحاته الفرعية أعلاه.
الصفحة الرئيسية تحتفظ بمعرّفات الأقسام (`#about` وغيرها) فتعمل الروابط القديمة المنتهية بها.

## 3. صفحات الموقع (`/Admin/Pages`)

القائمة الجانبية ← «الموقع» ← «صفحات الموقع». شجرة بكل الصفحات (عددها في العنوان) مع: الاسم، الرابط (يفتح الصفحة في
نافذة جديدة)، «في القائمة»، شارة «EN» (مكتملة حين يوجد عنوان الصفحة وعنوان ووصف محركات البحث بالإنجليزية)، وأزرار.

- **«في القائمة»:** اضغطوا الشارة «ظاهرة/مخفية» لإظهار الصفحة أو إخفائها من القائمة العلوية وقائمة الجوال والفوتر.
  الإخفاء **لا يحذف الصفحة**: تبقى متاحة برابطها (وتبقى في خريطة الموقع إن كان محتواها كافياً). إخفاء صفحة رئيسية في
  القائمة يخفي معها صفحاتها الفرعية. الصفحة الرئيسية دائماً ظاهرة. الافتراضي: ظاهرة.
- **▲ ▼:** يحرّكان الصفحة بين الصفحات التي تتبع الأب نفسه (لا تظهر للرئيسية).
- **تعديل / إنشاء:** صفحات الموقع الأصلية موجودة بعد النشر؛ يظهر «إنشاء» فقط لصفحة بلا سجل.

شاشة «تعديل صفحة» تحتوي بطاقتين:

**بطاقة «النصوص»** (عربي وإنجليزي لكل حقل):
- **عنوان الصفحة** (مطلوب): في القائمة والعنوان الرئيسي ومسار التنقل.
- **الملخص:** يظهر في بطاقات الصفحة ضمن المنظومة والقوائم، ويُستعمل وصفاً بديلاً لمحركات البحث.
- **النص التمهيدي:** تحت العنوان مباشرة.
- **النص الرئيسي:** سطر فارغ = فقرة جديدة.
- **نص «قريباً»** (يظهر ما دامت القائمة فارغة، أو ما دامت الصفحة بلا محتوى): نص الحالة الفارغة، انظر القسم 5.
- **«تظهر في القائمة الرئيسية والفوتر»:** هو مفتاح الإظهار نفسه، ولا يظهر للصفحة الرئيسية.

**بطاقة «محركات البحث»:** عنوان ووصف محركات البحث بالعربية والإنجليزية. المقترح: العنوان من 30 إلى 60 حرفاً والوصف من
70 إلى 160 (تنبيه ملوّن فقط، لا يمنع الحفظ). إن تُرك العنوان فارغاً يُستعمل عنوان الصفحة. **عنوان ووصف الصفحة الرئيسية
يُعدَّلان هنا أيضاً** (الصفحة «الرئيسية»).
**تظهر الصفحة الإنجليزية في محركات البحث فقط حين يُملأ العنوان والوصف الإنجليزيان**؛ وإلا تبقى خارج الفهرس (ولا تُربط
بالنسخة العربية). الأزرار: «حفظ التعديلات»، «رجوع»، «معاينة العربية»، «معاينة الإنجليزية».

أين تُعدَّل بقية محتويات كل صفحة؟

| الصفحة | أين يُعدَّل محتواها |
|---|---|
| الرئيسية | «النصوص العامة» (Hero، من نحن، عناوين الأقسام، التسجيل، التواصل…) + «الخدمات» + «أرقام الأثر» + الأخبار + الفعاليات + «الشركاء والرعاة» + «الأسئلة الشائعة» |
| من نحن | «النصوص العامة» (الرؤية والرسالة وكلمة المؤسس) + قوائم: القيم، لماذا إنوفست، القطاعات، شركاء النجاح + «أرقام الأثر» |
| منظومتنا | «الخدمات» + قوائم: ماذا نقدم، برامجنا، خدمات الجهات الحكومية، خدمات الجامعات |
| المنتدى | قائمة «مكونات المنتدى» + آخر ثلاث فعاليات |
| الفعاليات | «الفعاليات» (القسم 9) |
| الشركاء والرعاة (المنتدى) | «الشركاء والرعاة» + قائمة «الجهات التي نعمل معها» |
| شبكة المستثمرين | قوائم: خدمات المستثمرين، خطوات الانضمام (مع وصف)، خدمات الشركات الناشئة، القطاعات |
| الأخبار | «الأخبار» (القسم 8) |
| تواصل معنا | «النصوص العامة» ← «معلومات التواصل» + «الأسئلة الشائعة» |
| كل الصفحات | العناوين والملخص والنص التمهيدي والنص الرئيسي ونص «قريباً» وSEO من «صفحات الموقع» |

## 4. القوائم (`/Admin/ContentItems`)

«الموقع» ← «القوائم (القيم، القطاعات…)». قوائم نصية قصيرة تُعرض في الصفحات، **13 قائمة**: القيم الأساسية (من نحن)،
لماذا إنوفست (من نحن)، القطاعات التي نخدمها، ماذا نقدم (المنظومة)، برامجنا، مكونات المنتدى، خدمات المستثمرين، خدمات
الشركات الناشئة، خدمات الجهات الحكومية، خدمات الجامعات، الجهات التي نعمل معها، شركاء النجاح، خطوات الانضمام إلى
الشبكة. اختاروا القائمة من أعلى الشاشة، ثم:
- **إضافة:** النص بالعربية وبالإنجليزية (الإنجليزي اختياري)، وأيقونة من قائمة مغلقة (أو «بلا أيقونة»)، وفي «خطوات
  الانضمام» وصف إضافي أيضاً. العنصر الجديد يُضاف في آخر القائمة.
- **تعديل** (النص، الأيقونة، الترتيب، وخانة «ظاهر» التي تخفي العنصر دون حذفه)، **حذف** (نهائي).
- القائمة التي لا عناصر مفعّلة فيها لا تظهر في الصفحة. **عنوان القائمة وجملتها التمهيدية في الصفحة** يُعدَّلان من
  «النصوص العامة» ← بطاقة «عناوين أقسام الصفحات».

## 5. الحالات الفارغة («قريباً»)

صفحات بلا محتوى بعد (المتحدثون، الحلقات، الضيوف، الفرص، باقات الرعاية، وأي قائمة بلا عناصر مفعّلة كالفعاليات والأخبار
والشركاء) تعرض **نص «قريباً» الذي تكتبونه** في «تعديل صفحة»، مع زر واحد لخطوة تالية (مثل «تواصل معنا»)، ولا تكون صفحة
فارغة. اكتبوا النص بالعربية وبالإنجليزية.
- هذه الصفحات **خارج محركات البحث** (`noindex`) وخارج خريطة الموقع ما دام محتواها فارغاً (قائمة بلا عناصر مفعّلة، أو صفحة
  بلا نص تمهيدي ولا رئيسي)، وتدخل الفهرس تلقائياً عند ظهور المحتوى. المتحدثون والحلقات والضيوف والفرص لا تحتوي بعد على
  شاشة إدارة لإضافة صفوفها، فتبقى بنص «قريباً» حتى مرحلة لاحقة؛ أما إن كتبتم لها نصاً تمهيدياً أو رئيسياً فتُعامَل صفحة
  عادية.
- لإخفاء صفحة فارغة من القائمة كلياً استعملوا «في القائمة» (القسم 3).
- قسم «كلمة المؤسس» في «من نحن» **مخفي ما دام نص الكلمة فارغاً** («النصوص العامة» ← «كلمة المؤسس»).
- صفحتا «الانضمام» و«الرعاية» تعرضان حالياً زراً إلى «تواصل معنا» (نموذجاهما في مرحلة لاحقة).

## 6. النصوص العامة (`/Admin/Content/Edit`)

«الموقع» ← «النصوص العامة» (كانت «الصفحة الرئيسية»). بطاقات: الهيدر والأزرار، الهوية، Hero، من نحن، عناوين أقسام
الصفحة الرئيسية، عناوين أقسام الصفحات، كلمة المؤسس، التسجيل، معلومات التواصل، الفوتر، SEO وبيانات الموقع. لكل نص حقل عربي
وحقل إنجليزي ثم «حفظ».

- **الجزء المميز في العناوين:** ضعوا الجزء الذي يظهر بلون مميز بين `{` و`}`. يجب أن يحتوي النص على زوج واحد فقط
  من الأقواس، وإلا يُرفض الحفظ.
- **الرؤية والرسالة والقيم (الصفحة الرئيسية):** تظهر في ثلاث بطاقات متساوية في صف كامل العرض تحت قسم «من نحن». «نص القيم» يُعرض
  **شرائح (chips)** إذا فصلتم بين القيم بفاصلة عربية «،» أو لاتينية «,» (قيمتان على الأقل)، وتظهر ملاحظة بهذا المعنى تحت الحقل؛ وإلا يظهر
  فقرة عادية. نصا الرؤية والرسالة يظلان فقرتين دائماً ولا يُقسَّمان عند الفواصل. افصلوا القيم الإنجليزية بفاصلة أيضاً
  (النص الإنجليزي المبذور صُحّح ليعطي 7 شرائح كالعربي، ما لم يكن قد عُدّل يدوياً). صفحة «من نحن» تعرض قيمها من قائمة
  «القيم الأساسية» (القسم 4) لا من هذا الحقل.
- **سنوات الخبرة، الهاتف، البريد:** قيمة واحدة للغتين.
- **نص شارة الخبرة:** كل سطر يظهر في سطر.
- **حقوق النشر:** اكتبوا `{year}` ليُستبدل بالسنة الحالية.
- **رابط الجهة المطوّرة** (الفوتر): `https://` أو فارغ.
- **السطر العلوي في Hero:** اختياري، ولا يظهر في الصفحة الإنجليزية إن تُرك حقله الإنجليزي فارغاً.
- **زر الهيدر:** «نص زر الهيدر» و«الصفحة التي يفتحها زر الهيدر» (قائمة منسدلة بالصفحات التسع عشرة بعناوينها ومساراتها؛ وإن كانت القيمة المحفوظة غير صالحة
  يُستعمل `network.join`).
- **عدد عناصر كل صفحة من القوائم:** «عدد العناصر في كل صفحة من القوائم (الأخبار، الفعاليات…) (1–50)»، الافتراضي 12؛
  تُرقَّم الصفحات `?page=2`… ومن يطلب رقماً خارج النطاق يرى «غير موجود».
- **حفظ الشاشة لا يغيّر ما لم يُرسل:** أي مفتاح غير موجود في النموذج يبقى كما هو.

### رابط الموقع الأساسي (بطاقة SEO وبيانات الموقع)
الحقل «رابط الموقع الأساسي»: `https://النطاق` فقط، **بدون مسار ولا شرطة أخيرة** ولا `?` ولا `#` (القيمة المبذورة
`https://www.innovest-holding.com`). منه تُبنى وسوم canonical وhreflang و`og:url` وملف `/sitemap.xml` وسطر
`Sitemap:` في `/robots.txt`. إذا تُرك فارغاً في الإنتاج تختفي هذه الوسوم ويعيد `/sitemap.xml` الخطأ 404.
«صورة المشاركة» مسار داخل الموقع يبدأ بـ `/` (القيمة المبذورة صورة الشعار `/assets/brand/og-default.jpg`).

## 6ب. صور الموقع: الشعار وصورة الواجهة

«الموقع» ← «النصوص العامة»: في أعلى الشاشة نموذج رفع صغير لكل صورة من صور الموقع (يعمل بأداة الرفع نفسها وقيودها: JPG أو PNG أو WebP حتى 5 ميغابايت):
- **الشعار على خلفية داكنة:** في رأس الموقع وتذييله وبطاقة «من نحن» في الرئيسية. يُفضّل PNG أو WebP بخلفية شفافة.
- **الشعار على خلفية فاتحة:** شعار المؤسسة الذي تقرؤه محركات البحث (بيانات المؤسسة JSON-LD).
- **صورة الواجهة في الرئيسية:** تظهر بجانب عنوان الصفحة الرئيسية بدل المثلث البرتقالي المرسوم.
- **صورة المؤسس:** قسم «كلمة المؤسس» في «من نحن».
- **الاحتياط:** ما دام حقل الشعارين فارغاً يُستعمل الشعار المرفق مع الموقع، وما دامت صورة الواجهة فارغة يظهر المثلث. زر الحذف يعيد المرفق. لا يتغير
  شعار لوحة التحكم وصفحة الدخول ولا صورة المشاركة الافتراضية (`seo_og_image`) بهذه الشاشة.

## 7. روابط إضافية في القائمة (`/Admin/Nav`)

«الموقع» ← «روابط إضافية في القائمة». قائمة الموقع الأساسية صارت من «صفحات الموقع». هذه الشاشة لروابط **إضافية** فقط
(مثل رابط خارجي أو `#قسم`) تُضاف إلى القائمة والفوتر. لكل عنصر: الاسم بالعربية والإنجليزية، الرابط، «مفعّل» (يخفي العنصر
دون حذفه)، و«يظهر في «روابط سريعة» بالفوتر». العناصر الفرعية تُضاف وتُعدَّل وتُحذف من نفس الشاشة. عناصر القائمة القديمة
ذات الروابط `#قسم` الثمانية **عُطّلت ولم تُحذف** عند هذا الإصدار، وتظهر في اللوحة بحالة غير مفعّلة.

## 8. الخدمات والأرقام

- **«الخدمات»** (`/Admin/Platforms`، كانت «المنصات»): العنوان والوصف بالعربية والإنجليزية، **العنوان المختصر**
  (إن تُرك فارغاً يُستعمل الكامل)، **الرابط** لزر «اعرف المزيد» (**يختفي الزر إن تُرك فارغاً**)، و«ظاهرة/مخفية» (شارة
  تُضغط لإخفاء الخدمة من الموقع دون حذفها).
- **«أرقام الأثر»** (`/Admin/Stats`): التسمية بالعربية والإنجليزية، التسمية المختصرة، الرقم، واللاحقة (مثل `+`). الأرقام
  تظهر في الرئيسية وفي «من نحن».

## 9. الأخبار والفعاليات والشركاء وصفحات التفاصيل

لكل **خبر** و**فعالية** الآن صفحة خاصة: `/news/<الرابط-المختصر>` و`/forum/events/<الرابط-المختصر>` (وتحت `/en`). تظهر
بطاقات القوائم والرئيسية بروابط إلى هذه الصفحات.

- **«الأخبار»**: إضافة وتعديل وحذف. الحقول: العنوان، **الرابط المختصر**، الملخص (البطاقة وأول فقرة)، **نص الخبر** (سطر فارغ =
  فقرة جديدة)، الفئة، التاريخ كما يظهر (**نص حر**)، **تاريخ النشر** (لمحركات البحث)، **رابط المصدر** (اختياري: يظهر زر «المصدر»
  في صفحة الخبر)، **صورة الخبر**، وعنوان ووصف محركات البحث. «الترتيب»: الرقم الأصغر أولاً. «مفعّل» يخفي الخبر دون حذفه.
  الخبر الجديد يظهر أولاً. تظهر الأخبار في `/news` مرقّمة، وفي الرئيسية أول **عدد الأخبار المعروضة في الصفحة الرئيسية** (النصوص
  العامة ← «عناوين أقسام الصفحة الرئيسية»، رقم من 1 إلى 50، الافتراضي 6).
- **«الفعاليات»** (ضمن «منتدى إنوفست العربي»): العنوان، **الرابط المختصر**، الملخص، **الوصف** (نص صفحة الفعالية)، **الموعد**:
  البدء (بتوقيت مكان الفعالية) والانتهاء (اختياري) و**فرق التوقيت عن UTC** (الافتراضي `+02:00`)، **الحالة** (في موعدها / مؤجلة
  / ملغاة)، **طريقة الحضور** (حضوري / عن بُعد / حضوري وعن بُعد)، اسم المكان والمدينة والعنوان، **رابط الحضور عن بُعد**
  (`https://`)، **«التسجيل مفتوح»**، **صورة الفعالية**، ومحركات البحث. اليوم والشهر والوقت النصية حقول بديلة للفعاليات القديمة
  غير المؤرَّخة. في القوائم تظهر الفعاليات القادمة أولاً (الفعاليات القديمة غير المؤرَّخة قبلها) ثم المنتهية، وتحمل المنتهية
  والملغاة شارة، ويختفي زر التسجيل تلقائياً بعد الانتهاء أو الإلغاء أو إغلاق التسجيل. تظهر الفعاليات في `/forum/events`، وأول ثلاث
  في صفحة المنتدى.
- **«الشركاء والرعاة»**: الاسم بالعربية والإنجليزية، **شعار الشريك** (صورة)، **موقع الشريك** (`https://` اختياري؛ يُفتح برابط
  `rel="sponsored noopener"`)، **المجموعة**، و**«يظهر في شريط الشركاء في الصفحة الرئيسية»**. **المجموعات** (بطاقة في أسفل
  الشاشة): عناوين تُرتَّب بها صفحة `/forum/partners` (مثل «الرعاة الرئيسيون»)؛ حذف مجموعة **لا يحذف شركاءها** بل يُعرضون بعد
  المجموعات. إخفاء شريك دون حذفه بشارة «ظاهر/مخفي».
- **الرابط المختصر:** أحرف إنجليزية صغيرة وأرقام وشرطات فقط (`innovest-forum-2026`)، حتى 100 حرف، وبعض الكلمات محجوزة (مثل
  `admin` و`media` و`en`). إن تُرك فارغاً عند الإضافة يُبنى من العنوان الإنجليزي (ويُضاف `-2` عند التكرار)، وإلا `news-<رقم>` أو
  `event-<رقم>`. **تغييره لاحقاً آمن:** يبقى الرابط القديم يحوّل (301) إلى الجديد، ولا يمكن لعنصر آخر استعمال رابط قديم لعنصر
  موجود. الأخبار والفعاليات الموجودة أخذت روابط `news-<رقم>` و`event-<رقم>` تلقائياً عند هذا الإصدار.
- **الإنجليزية:** إن لم يوجد للعنصر عنوان وملخص بالإنجليزية تعرض صفحته الإنجليزية النص العربي مع تنبيه، وتكون **خارج محركات
  البحث** (`noindex`) وخارج خريطة الموقع، وتشير canonical إلى الصفحة العربية؛ تدخل الفهرس حين يُملأ العنوان والملخص الإنجليزيان.
- حذف خبر أو فعالية يحذف صفحته وصورته وروابطها القديمة.
- الأخبار والفعاليات والشركاء الحالية **محفوظة كما هي**، وكل الشركاء يظهرون في الرئيسية بعد الإصدار.

### رفع الصور

حقل «صورة…» موجود في الأخبار والفعاليات والشركاء («الشعار») وصفحات الموقع (تظهر في بطاقة الصفحة وفي مشاركتها) وكلمة المؤسس
(«النصوص العامة»).
- **الأنواع:** JPG أو PNG أو WebP فقط. لا SVG ولا GIF ولا ملفات أخرى حتى لو غُيّر امتدادها: يُفحص محتوى الملف نفسه، ويجب أن
  يتفق الامتداد والنوع والمحتوى.
- **الأحجام:** حتى **5 ميغابايت** للملف؛ من **16 إلى 8000 بكسل** لكل ضلع، وبما لا يتجاوز **25 مليون بكسل**. الصور المتحركة
  (APNG وWebP المتحرك) مرفوضة. عند الرفض تظهر رسالة تحت الحقل ولا يُحفظ شيء.
- **ما يحدث للملف:** يُعاد بناء الملف بعناصر الصورة فقط؛ تُحذف بيانات الموقع الجغرافي والكاميرا (EXIF) والتعليقات وأي بيانات
  بعد نهاية الصورة، ويبقى اتجاه الصورة وملف الألوان. لا تتغير البكسلات ولا يُضغط الملف من جديد، فارفعوا صورة بحجم مناسب (وزنها
  هو وزن الصفحة). اسم الملف الأصلي لا يُستعمل أبداً؛ يُعطى الملف مفتاحاً عشوائياً ويُعرض من `/media/<مفتاح>.<امتداد>`.
- **الاستبدال والحذف:** اختاروا ملفاً جديداً تحت «استبدال الصورة» ثم احفظوا، فيُحذف الملف القديم. لإزالة الصورة علّموا
  «حذف الصورة الحالية» ثم احفظوا. تُحذف الملفات من الخادم بعد نجاح الحفظ؛ وإن فشل الحفظ لا يبقى الملف الجديد.
- **النص البديل:** وصف الصورة لقارئ الشاشة بالعربية والإنجليزية؛ إن تُرك فارغاً يُستعمل العنوان.
- الصورة الأعرض من 1200 بكسل تُستعمل بحجم كبير في المشاركة على وسائل التواصل؛ وإلا تُستعمل صورة الصفحة ثم الصورة الافتراضية.
- لا ترفعوا صوراً تحتوي بيانات خاصة؛ كل ما يُرفع يظهر للعامة.

## 9ب. المتحدثون وضيوف البرنامج والحلقات والفرص وباقات الرعاية

في القائمة الجانبية: مجموعة «منتدى إنوفست العربي» ← **المتحدثون**؛ مجموعة «شبكة المستثمرين الملائكيين» ← **الفرص الاستثمارية**؛ مجموعة
«البرنامج التلفزيوني» ← **الحلقات** و**الضيوف** و**باقات الرعاية**. تحل هذه الشاشات محل نص «قريباً» في صفحات `/forum/speakers`
و`/tv/episodes` و`/tv/guests` و`/angel-network/opportunities` (وتحت `/en`)، ولكل عنصر صفحة خاصة بالرابط المختصر.

- **قاعدة مشتركة:** عند إضافة عنصر لأول مرة لا تعود الصفحة «فارغة»: تُفهرس تلقائياً (القسم 5). ما دامت القائمة بلا عناصر منشورة تعرض الصفحة
  نص «قريباً» (يُعدَّل من «صفحات الموقع») وتبقى `noindex`. العناصر المخفية لا تظهر في أي مكان. **الرابط المختصر** والاستبدال/الحذف
  للصور كما في الأخبار (القسم 9): أحرف إنجليزية صغيرة وأرقام وشرطات؛ تغييره يُبقي القديم يحوّل (301). صفحة العنصر الإنجليزية تدخل محركات
  البحث فقط حين تتوفر نصوصه الإنجليزية، وإلا تعرض العربية مع تنبيه وتكون `noindex`. أزرار «ظاهر/مخفي» تخفي العنصر دون حذفه، وحقل «الترتيب»
  (الأصغر أولاً) يحدد الترتيب.
- **«المتحدثون» و«الضيوف»** (الحقول نفسها): الاسم، المنصب، الجهة، نبذة، رابط لينكدإن، رابط X (`https://` اختياريان)، **الصورة الشخصية**،
  الرابط المختصر، محركات البحث، الترتيب، ظاهر/مخفي. تظهر الصفحة الإنجليزية في الفهرس حين يُملأ الاسم الإنجليزي (والمنصب والنبذة بالإنجليزية إن
  وُجدت بالعربية). حذف متحدث يُزيله من فعالياته؛ حذف ضيف يُزيله من حلقاته. **ملف بلا وصف** (لا نبذة ولا وصف لمحركات البحث) لا يدخل محركات البحث (`noindex`) ولا خريطة الموقع بأي لغة، ويحتاج الإنجليزي وصفه الإنجليزي بنفسه؛ أضيفوا نبذة ليُفهرس. وتظهر في صفحة المتحدث أو الضيف الفعاليات والحلقات **المنشورة فقط**.
- **ربط المتحدثين بالفعاليات:** في شاشة تعديل «الفعاليات» مجموعة «متحدثو الفعالية (يظهرون في صفحتها بترتيب قائمة «المتحدثون»)» بمربعات اختيار؛ ترتيب الظهور هو **ترتيب قائمة المتحدثين** (حقل «الترتيب» ثم الأقدم إضافةً) لا ترتيب النقر؛ تظهر
  في صفحة الفعالية المتحدثون المنشورون فقط، وفي صفحة المتحدث فعالياته. المتحدث المعطَّل يبقى في القائمة بعلامة ولا يظهر للزوار.
- **«الحلقات»:** عنوان الحلقة، الملخص، الوصف، **الموسم** و**رقم الحلقة** و**تاريخ العرض** (اختيارية)، **رابط الفيديو**، **الصورة المصغّرة**
  (ملصق الفيديو وصورة المشاركة)، **ضيوف الحلقة** (مربعات اختيار من «الضيوف»؛ يظهرون بترتيب قائمة «الضيوف»: «الترتيب» ثم الأقدم إضافةً)، الرابط المختصر، محركات البحث، الترتيب. ترتَّب
  القائمة: الحلقات ذات تاريخ العرض الأحدث أولاً ثم غير المؤرَّخة.
  - **رابط الفيديو** `https://` فقط. رابط **يوتيوب** (`youtube.com/watch?v=…` أو `youtu.be/…` أو `/embed/` أو `/shorts/` أو `/live/` من المواقع
    youtube.com وyoutube-nocookie.com وyoutu.be) يظهر بمشغّل **لا يتصل بيوتيوب إلا عندما يضغط الزائر عليه** (يُستعمل نطاق youtube-nocookie)؛
    أي رابط آخر `https://` يظهر زر «شاهد الحلقة». لا يظهر مشغّل إن تُرك الحقل فارغاً.
- **«الفرص الاستثمارية»:** عنوان الفرصة، الملخص، التفاصيل، **القطاع** (يُختار من قائمة «القطاعات التي نخدمها»، أو «بدون»)، المرحلة والتمويل
  المطلوب (**نصان حران**، مثل «عند الطلب»؛ لا يظهر أي رقم لم تكتبوه)، **«متاحة لاستقبال الاهتمام»**، الصورة، الرابط المختصر، محركات البحث.
  - **المسودة والنشر:** تُحفظ الفرصة الجديدة **مسودة** (شارة «مسودة») ولا تظهر في الموقع (ولا في خريطة الموقع، ورابطها 404 للزوار) حتى تُفعّل
    «منشورة». الفرصة المنشورة المغلقة (إن أُلغيت «متاحة…») تظهر بعلامة «مغلقة» بلا زر اهتمام. تُرتَّب المتاحة أولاً.
  - زر «أبدِ اهتمامك» في صفحة الفرصة يفتح صفحة «تواصل معنا» ريثما يُضاف نموذج خاص.
  - **حذف قطاع** من «القطاعات التي نخدمها» مستعمل في فرصة يُرفض برسالة («عطّله بدلاً من حذفه»).
- **«باقات الرعاية»:** الوحدة (البرنامج التلفزيوني أو المنتدى)، اسم الباقة، **السعر** (نص حر مثل «عند الطلب»)، **المزايا** (ميزة في كل سطر)،
  الترتيب، ظاهرة/مخفية. باقات البرنامج التلفزيوني تظهر في صفحة «الرعاية» (`/tv/sponsorship`) وباقات المنتدى في «الشركاء والرعاة»
  (`/forum/partners`).
- **صفحة المركز وإخفاؤها:** إخفاء صفحة (مثل «الحلقات») من القائمة يتم من «صفحات الموقع» ← «في القائمة» (القسم 3)؛ وتبقى الصفحة متاحة برابطها.
  صفحتا `/tv` و`/tv/sponsorship` تبقيان `noindex` ما دام نصهما التمهيدي والرئيسي فارغين.
- **الصور:** كل ما سبق يستعمل أداة الرفع نفسها وقيودها (JPG وPNG وWebP حتى 5 ميغابايت، القسم «رفع الصور»)؛ الصورة تُحذف مع العنصر.

## 10. الأسئلة الشائعة

«الأسئلة الشائعة»: السؤال والجواب بالعربية والإنجليزية، «الترتيب»، «مفعّل». تظهر في الرئيسية وفي «تواصل معنا». إذا لم يبقَ
سؤال مفعّل يختفي القسم ورابطه من الصفحة.

## 11. خيارات مجال الاهتمام (قائمة نموذج التسجيل)

«خيارات مجال الاهتمام»: الاسم بالعربية والإنجليزية، الترتيب، مفعّل. عند التسجيل يُحفظ **اسم الخيار بالعربية**
مع التسجيل، فلا يتغير سجل قديم إن عدّلتم الخيار أو حذفتموه لاحقاً. القيمة غير الموجودة في القائمة تُرفض.

## 12. مزايا التسجيل

«مزايا التسجيل»: النقاط التي تظهر بجانب نموذج التسجيل. النص بالعربية والإنجليزية، الترتيب، مفعّل. إن لم تبقَ نقطة
مفعّلة يختفي الصندوق.

## 13. روابط التواصل الاجتماعي

«روابط التواصل الاجتماعي»: سبع شبكات ثابتة (LinkedIn وX/Twitter وInstagram وYouTube وFacebook وTikTok وLinktree). لكل
شبكة: الرابط (`https://` كامل)، الترتيب، مفعّل. **الشبكة بلا رابط لا تظهر** في الفوتر. تُضاف الروابط المفعّلة أيضاً إلى
البيانات المنظمة (`sameAs`) للموقع.

## 14. صندوق الطلبات والتسجيلات ورسائل التواصل

### «الطلبات (كل النماذج)» (`/Admin/Leads`)
القائمة الجانبية ← «الوارد» ← «الطلبات (كل النماذج)». صندوق واحد لكل ما يرسله الزوار عبر نماذج الموقع، **الأحدث أولاً، 50 في الصفحة**
مع تنقل بين الصفحات. بجانب «الطلبات» و«رسائل التواصل» في القائمة شارة بعدد **غير المقروء**، وفي «نظرة عامة» بطاقة بأعداد غير
المقروء لكل نوع.
- **الأنواع** (مرشّح «النوع»): تسجيل في المنتدى · انضمام — مستثمر · انضمام — شركة ناشئة · استفسار رعاية · اهتمام بالفرص · تسجيل
  سريع (الرئيسية). **مرشّح «القراءة»:** الكل / غير مقروء / مقروء، و**مرشّح «الحالة»:** الكل / جديد / تم التواصل / مغلق. تُجمع المرشّحات الثلاثة معاً، وتبقى بعد تعليم الطلب أو حفظه أو حذفه.
- **الجدول:** القراءة («جديد» أو «مقروء»)، شارة **الحالة** (جديد / تم التواصل / مغلق)، علامة ✎ إن وُجدت ملاحظة، النوع، الاسم، البريد، التفاصيل (جهة أو نوع أو فعالية…)، لغة الصفحة التي أُرسل منها النموذج،
  التاريخ (بتوقيت متصفحكم؛ مرّروا المؤشر لترى UTC)، والإجراءات.
- **التفاصيل:** تفتح كل حقول الطلب. زر **«مقروء/غير مقروء»** يبدّل الحالة (لا يُعلَّم الطلب مقروءاً تلقائياً عند فتحه)، وزر **الحذف**
  بعد تأكيد؛ **الحذف نهائي**.
- **حالة المتابعة والملاحظة الداخلية** (في شاشة التفاصيل): اختاروا الحالة — **جديد** (كل الطلبات عند وصولها وكل الطلبات القديمة) ثم **تم التواصل** ثم **مغلق** — واكتبوا **ملاحظة داخلية** (حتى 2000 حرف، مثل «اتصلنا به يوم الاثنين») ثم «حفظ». الحالة والملاحظة **لا يراها الزائر أبداً** ولا يمكن إرسالهما من نماذج الموقع، وهما منفصلتان عن «مقروء/غير مقروء». كل تغيير يُحفظ بزر واحد.
- **التصدير إلى Excel (CSV):** رابط «تصدير إلى Excel (CSV)» في رأس القائمة ينزّل ملف `leads-<النوع أو all>-<التاريخ>.csv` يضم **الطلبات التي يطابقها المرشّح الحالي** (النوع والقراءة والحالة) من الأحدث إلى الأقدم، دون ترقيم صفحات. الأعمدة: النوع، الرقم، التاريخ (UTC)، الاسم، البريد، الهاتف، التفاصيل، الحالة، مقروء، لغة الصفحة، الملاحظة الداخلية. يُحفظ الملف بترميز UTF-8 مع BOM فيُفتح بالعربية الصحيحة بنقرتين في Excel، وكل خلية بين علامتي اقتباس. **القيمة التي تبدأ بـ `=` أو `+` أو `-` أو `@` أو `|` (أو بصيغها العريضة ＝ ＋ － ＠، ولو سبقتها مسافات أو مسافة جدولة) تُسبَق بعلامة `'`** حتى لا تُنفَّذ كصيغة في Excel؛ ستراها في الخلية بعلامة `'` في أولها (مثل `'=SUM(1)`) وهذا مقصود. التواريخ نص بتوقيت UTC. الملف المنزَّل **بيانات شخصية**: عاملوه كقاعدة البيانات، ولا تتركوه في مجلد التنزيلات أو تُرسلوه بقناة غير آمنة.
- ما يجمعه كل نموذج (النماذج الأربعة الأولى تتطلب الاسم والبريد وتعليم مربع الموافقة؛ الهاتف اختياري):
  - **التسجيل في المنتدى** (`/forum/registration`): الجهة، المسمى الوظيفي، الدولة، مجال الاهتمام (من «خيارات مجال الاهتمام»)،
    والفعالية (اختيارية؛ فعالية مفعّلة فقط، وإلا «أي فعالية»).
  - **الانضمام** (`/angel-network/join`): النوع (مستثمر أو شركة ناشئة) والدولة (إلزاميان). المستثمر: الجهة، نوع المستثمر، حجم
    الاستثمار، القطاعات، رابط LinkedIn (`https://`)، رسالة. الشركة الناشئة: **اسم الشركة والقطاع والمرحلة إلزامية**، والتمويل المطلوب،
    الموقع (`https://`)، نبذة.
  - **الرعاية** (`/tv/sponsorship`): اسم الشركة ومسؤول التواصل والجهة المراد رعايتها (المنتدى أو التلفزيون)، الباقة (إن وُجدت باقات)، رسالة.
  - **اهتمام بالفرص** (`/angel-network/opportunities`): الاسم والبريد والهاتف ورسالة؛ الفرصة «عموماً» ما دامت صفحات الفرص غير متوفرة.
  - **التسجيل السريع** في الصفحة الرئيسية كما كان (الاسم والبريد والهاتف ومجال الاهتمام).
  يُحفظ مع كل طلب: وقت الموافقة ولغة الصفحة ووقت الإرسال. القوائم المنسدلة تُحفَظ بنصها العربي وقت الإرسال، فلا يتغير طلب قديم إن
  عدّلتم الخيارات لاحقاً.
- **تعديل نصوص النماذج وخياراتها:** «النصوص العامة» ← بطاقة «نماذج الطلبات» (نص الموافقة، وعناوين ونصوص أعمدة الانضمام والرعاية
  والاهتمام)؛ و«القوائم» ← أربع قوائم: أنواع المستثمرين، حجم الاستثمار، مراحل الشركة الناشئة، التمويل المطلوب (القطاعات من قائمة
  «القطاعات التي نخدمها»). الخيار المخفي لا يظهر في النموذج.
- صفحة «الفرص» تعرض نموذج الاهتمام **تحت نص «قريباً»** دون أن تُعدّ الصفحة ذات محتوى (تبقى `noindex`). وصفحة «الرعاية» تدخل محركات
  البحث بعد أن أصبحت لها فقرة تمهيدية.
- لا يُرسَل أي بريد عند وصول طلب: راجعوا الصندوق (الشارة) دورياً.

### بيانات شخصية (PII)
الطلبات بيانات شخصية: **تظهر لمن يدخل اللوحة فقط** ولا تظهر في الموقع العام. لا يسجّل التطبيق أي بيانات من النماذج في سجلاته. لا يُحفظ عنوان
IP ولا متصفح الزائر مع الطلب. احذفوا ما لم تعد تحتاجونه، ولا تنسخوه إلى قنوات غير آمنة. التصدير إلى CSV متاح للمدير فقط (انظر أعلاه).

### «التسجيلات» و«رسائل التواصل»
«التسجيلات» تعرض كل التسجيلات (مع عمود «النموذج»: سريع أو منتدى) و«رسائل التواصل» رسائل صفحة التواصل: الأحدث أولاً، **50 في الصفحة**. تظهر
لغة الصفحة (عربي/إنجليزي). يمكن تعليم الرسالة «مقروء» أو حذفها، والحذف نهائي. النماذج تُرسَل إلى `/forms/…` (أو `/en/forms/…`).

## 15. تغيير كلمة المرور

«الإعدادات»: أدخلوا كلمة المرور الحالية ثم الجديدة (من 10 إلى 64 حرفاً) وتأكيدها. تنتهي بقية الجلسات وتبقى جلستكم.
المحاولات الخاطئة تُحتسب: 5 محاولات فاشلة على الحساب خلال 15 دقيقة تقفله 15 دقيقة.

## 16. عرض الموقع

أسفل القائمة الجانبية: «عرض الموقع» (`/`) و«عرض النسخة الإنجليزية» (`/en`). راجعوا الصفحتين بعد كل تعديل.
الشعار الحالي صورة نقطية مستخرجة من ملفي الشعار؛ ملف الشعار المتجهي (SVG) لم يُسلَّم بعد.

---

# English

The panel itself is in Arabic. Every public text has two values: Arabic (shown on `/…`) and English (shown on `/en/…`).
The Arabic field is usually required, the English field optional. Each field carries an «ع» or «EN» chip so you know
which language you are typing.

## 1. General rules

- **Every admin link starts with `/Admin/`** (e.g. `/Admin/Pages`, `/Admin/Events`, `/Admin/Settings`). The old links
  without `/Admin` (e.g. `/Dashboard`, `/Events`) no longer work and show the "not found" page; update bookmarks.
  Sign in at `/Account/Login`.
- **Fallback:** if an English field is left empty, the English page shows the Arabic text instead (displayed
  right-to-left). The Arabic page never shows English. A few fields have no fallback (e.g. the Hero kicker): they stay
  empty on the English page when their English field is empty.
- **Changes are live immediately:** after saving, both languages show the change on the next request. No restart needed.
- **The current English text is a draft** pending review (where the company profile has no English, it is an
  approximate gloss); edit it from the screens below.
- **Neutral fields** (phone, e-mail, URLs) have one value for both languages and are typed left-to-right.
- **Times** in «التسجيلات» and «رسائل التواصل» are stored in UTC and shown in **the time zone of the viewer's
  browser**, in the page's language. With JavaScript off the UTC value is shown, labelled UTC. Hover over a time to
  see its UTC value.
- **Links:** social-media links and the developer-credit link must start with `https://`. Menu, service and news
  links accept `#section`, `/path` or `https://…`; `javascript:` and similar are refused. Leave the field empty when
  there is no link.
- Never type passwords or secrets into any field: whatever is entered as content is public.
- **The sidebar is grouped:** «نظرة عامة», «الموقع», «منتدى إنوفست العربي», «الأخبار», «الوارد». Click a group title to
  collapse or open it; the browser remembers your choice.

## 2. Site structure

The site is now multi-page. **Page addresses (URLs) are fixed in code and cannot be added or removed from the panel**;
their texts, menu visibility, order and search-engine data are edited in the panel. Every page has an English twin at
the same path after `/en`.

| Page | Arabic URL | English URL |
|---|---|---|
| Home | `/` | `/en` |
| About | `/about` | `/en/about` |
| Ecosystem | `/ecosystem` | `/en/ecosystem` |
| Forum | `/forum` | `/en/forum` |
| ↳ Events | `/forum/events` | `/en/forum/events` |
| ↳ Speakers | `/forum/speakers` | `/en/forum/speakers` |
| ↳ Partners and sponsors | `/forum/partners` | `/en/forum/partners` |
| ↳ Registration | `/forum/registration` | `/en/forum/registration` |
| Angel network | `/angel-network` | `/en/angel-network` |
| ↳ Investors | `/angel-network/investors` | `/en/angel-network/investors` |
| ↳ Startups | `/angel-network/startups` | `/en/angel-network/startups` |
| ↳ Opportunities | `/angel-network/opportunities` | `/en/angel-network/opportunities` |
| ↳ Join | `/angel-network/join` | `/en/angel-network/join` |
| TV | `/tv` | `/en/tv` |
| ↳ Episodes | `/tv/episodes` | `/en/tv/episodes` |
| ↳ Guests | `/tv/guests` | `/en/tv/guests` |
| ↳ Sponsorship | `/tv/sponsorship` | `/en/tv/sponsorship` |
| News | `/news` | `/en/news` |
| Contact | `/contact` | `/en/contact` |

(The name shown on the site comes from the «page title» field and may differ from the first column.) The tree: Home →
About, Ecosystem, News, Contact; Ecosystem → Forum, Angel network, TV; each with the sub-pages above. The home page
keeps its section ids (`#about` etc.), so old links ending in them still work.

## 3. Site pages (`/Admin/Pages`)

Sidebar → «الموقع» → «صفحات الموقع». A tree of all pages (the count is in the heading) showing: name, URL (opens the page
in a new tab), «in menu», an «EN» chip (complete when the page title and the English search title and description exist),
and buttons.

- **«In menu»:** click the «ظاهرة/مخفية» badge to show or hide the page in the top menu, the mobile drawer and the footer.
  Hiding **does not delete the page**: it stays reachable by its URL (and stays in the sitemap if it has enough content).
  Hiding a top-level page also hides its sub-pages from the menu. The home page is always shown. Default: shown.
- **▲ ▼:** move the page among the pages that share its parent (not shown for the home page).
- **Edit / Create:** the site's pages exist after the deploy; «إنشاء» appears only for a page with no row.

The «edit page» screen has two cards:

**«النصوص» card** (Arabic and English for each field):
- **Page title** (required): menu, main heading and breadcrumb.
- **Summary:** shown on the page cards in the ecosystem and menus; also the fallback description for search engines.
- **Intro text:** right under the heading.
- **Main text:** a blank line starts a new paragraph.
- **«Coming soon» text** (shown while the list is empty, or while the page has no content): the empty-state text, see
  section 5.
- **«تظهر في القائمة الرئيسية والفوتر»:** the same show-in-menu switch; absent for the home page.

**«محركات البحث» card:** search-engine title and description in Arabic and English. Suggested: title 30-60
characters, description 70-160 (a coloured hint only; it never blocks saving). If the title is empty the page title is
used. **The home page's title and description are edited here too** (the «Home» page).
**The English page is offered to search engines only when the English title and description are both filled**;
otherwise it stays out of the index (and is not linked to the Arabic page). Buttons: «حفظ التعديلات», «رجوع»,
«معاينة العربية», «معاينة الإنجليزية».

Where is the rest of each page's content edited?

| Page | Where its content is edited |
|---|---|
| Home | «النصوص العامة» (Hero, About, section headings, register, contact…) + «الخدمات» + «أرقام الأثر» + News + Events + «الشركاء والرعاة» + «الأسئلة الشائعة» |
| About | «النصوص العامة» (vision, mission, founder's message) + lists: values, why Innovest, sectors, partners in success + «أرقام الأثر» |
| Ecosystem | «الخدمات» + lists: what we offer, programs, government services, university services |
| Forum | list «مكونات المنتدى» + the latest three events |
| Events | «الفعاليات» (section 9) |
| Partners (forum) | «الشركاء والرعاة» + list «الجهات التي نعمل معها» |
| Angel network | lists: investor services, join steps (with description), startup services, sectors |
| News | «الأخبار» (section 9) |
| Contact | «النصوص العامة» → «معلومات التواصل» + «الأسئلة الشائعة» |
| All pages | titles, summary, intro, main text, «coming soon» text and SEO from «صفحات الموقع» |

## 4. Lists (`/Admin/ContentItems`)

«الموقع» → «القوائم (القيم، القطاعات…)». Short text lists shown on the pages, **13 lists**: core values (About), why
Innovest (About), sectors we serve, what we offer (ecosystem), our programs, forum components, investor services, startup
services, government services, university services, organisations we work with, partners in success, steps to join the
network. Pick the list at the top, then:
- **Add:** text in Arabic and English (English optional), an icon from a fixed list (or «no icon»), and, for «join
  steps», an extra description. A new item goes to the end of the list.
- **Edit** (text, icon, order, and the «ظاهر» checkbox, which hides an item without deleting it), **delete** (permanent).
- A list with no active item is not shown on the page. **The list's heading and lead sentence on the page** are edited in
  «النصوص العامة» → card «عناوين أقسام الصفحات».

## 5. Empty states («coming soon»)

Pages with no content yet (Speakers, Episodes, Guests, Opportunities, Sponsorship, and any list with no active item such as
events, news or partners) show **the «coming soon» text you write** in «edit page», plus one next-step button (e.g.
«Contact»), so they are never blank. Write it in Arabic and English.
- These pages are **kept out of search engines** (`noindex`) and out of the sitemap while their content is empty (a list
  with no active item, or a page with neither intro nor main text), and enter the index automatically once content
  appears. Speakers, Episodes, Guests and Opportunities have no admin screen yet for adding their rows, so they stay
  on the «coming soon» text until a later phase; if you write an intro or main text for them they are treated as ordinary
  pages.
- To hide an empty page from the menu altogether use «in menu» (section 3).
- The «founder's message» block on About **stays hidden while the message text is empty** («النصوص العامة» → «كلمة المؤسس»).
- The Join and Sponsorship pages currently show a button to «Contact» (their forms come in a later phase).

## 6. General texts (`/Admin/Content/Edit`)

«الموقع» → «النصوص العامة» (formerly «الصفحة الرئيسية»). Cards: header and buttons, brand, Hero, About, home-page section
headings, page-section headings, founder's message, register section, contact details, footer, SEO and site data. Each text
has an Arabic and an English field, then Save.

- **Highlighted part of a heading:** put the part shown in the accent colour between `{` and `}`. Use exactly one pair of
  braces or the save is refused.
- **Vision, mission and values (home page):** shown as three equal cards in a full-width row under the About section. The
  «نص القيم» text is shown as **chips** when you separate the values with an Arabic comma «،» or a Latin comma «,» (at
  least two values); a hint under the field says so. Otherwise it shows as a paragraph. Vision and mission always stay
  paragraphs and are never split at commas. Separate the English values with commas too (the seeded English text was
  corrected to give 7 chips like the Arabic, unless it had been edited by hand). The About page takes its values from the «core values» list (section 4),
  not from this field.
- **Years of experience, phone, e-mail:** one value for both languages.
- **Experience badge text:** each line is shown on its own line.
- **Copyright:** type `{year}` to insert the current year.
- **Developer credit link** (footer): `https://` or empty.
- **Hero kicker:** optional; hidden on the English page when its English field is empty.
- **Header button:** «نص زر الهيدر» and «الصفحة التي يفتحها زر الهيدر» (a drop-down of the 19 pages with their Arabic titles and paths; an invalid stored value
  falls back to `network.join`).
- **Items per page of the lists:** «عدد العناصر في كل صفحة من القوائم (الأخبار، الفعاليات…) (1–50)», default 12; pages are
  numbered `?page=2`…; a number out of range shows "not found".
- **Saving does not change what is not posted:** any key missing from the form stays as it was.

### Base URL (SEO and site data card)
The field «رابط الموقع الأساسي» takes `https://domain` only, **no path, no trailing slash**, no `?` and no `#` (seeded
value `https://www.innovest-holding.com`). Canonical, hreflang, `og:url`, `/sitemap.xml` and the `Sitemap:` line of
`/robots.txt` are built from it. If it is emptied in production those tags disappear and `/sitemap.xml` returns 404.
«Share image» is a site path starting with `/` (seeded with the logo image `/assets/brand/og-default.jpg`).

## 6b. Site images: the logo and the hero image

«الموقع» → «النصوص العامة»: at the top of the screen there is one small upload form per site image (same upload tool and limits: JPG,
PNG or WebP up to 5 MB):
- **Logo on a dark background:** the site header, the footer and the home «About» card. PNG or WebP with a transparent background is best.
- **Logo on a light background:** the organisation logo that search engines read (the Organization JSON-LD).
- **Home hero image:** shown beside the home page heading instead of the drawn orange triangle.
- **Founder photo:** the «founder's message» block on About.
- **Fallback:** while a logo field is empty the logo bundled with the site is used, and while the hero image is empty the triangle is shown.
  Removing an image brings the bundled one back. The admin panel and login logo and the default share image (`seo_og_image`) are not changed by this screen.

## 7. Extra menu links (`/Admin/Nav`)

«الموقع» → «روابط إضافية في القائمة». The site's main menu now comes from «صفحات الموقع». This screen is only for **extra**
links (e.g. an external link or `#section`) added to the menu and the footer. Per item: name in Arabic and English, link,
«active» (hides the item without deleting it) and «shown in footer quick links». Sub-items are added, edited and deleted on
the same screen. The eight old `#section` menu items were **deactivated, not deleted**, by this release and show as inactive.

## 8. Services and figures

- **«الخدمات»** (`/Admin/Platforms`, formerly «Platforms»): title and description in both languages, a **short title** (the
  full title is used when empty), the **link** of the «اعرف المزيد» button (**the button is hidden when the link is
  empty**), and a «ظاهرة/مخفية» badge (click to hide the service from the site without deleting it).
- **«أرقام الأثر»** (`/Admin/Stats`): label in both languages, short label, number and suffix (e.g. `+`). The figures
  appear on the home page and on About.

## 9. News, events, partners and detail pages

Every **news item** and **event** now has its own page: `/news/<slug>` and `/forum/events/<slug>` (and under `/en`). The
cards in the lists and on the home page link to these pages.

- **«الأخبار»**: add, edit, delete. Fields: title, **slug**, summary (card and first paragraph), **article text** (a blank
  line starts a new paragraph), category, date as shown (**free text**), **published date** (for search engines), **source
  link** (optional: shows a «source» button on the article), **news image**, and search-engine title and description.
  «Order»: lower number first. «Active» hides an item without deleting it. A new item appears first. News is listed on
  `/news` with paging, and the home page shows the first **number of news items on the home page** (General texts →
  «عناوين أقسام الصفحة الرئيسية», a whole number from 1 to 50, default 6).
- **«الفعاليات»** (under «منتدى إنوفست العربي»): title, **slug**, summary, **description** (the event page text), **date and
  time**: start (in the event's local time), optional end and the **UTC offset** (default `+02:00`), **status** (scheduled /
  postponed / cancelled), **attendance mode** (in person / online / mixed), venue name, city and address, **online link**
  (`https://`), **«registration open»**, **event image**, and search engines. The day, month and time text fields remain
  as a fallback for old events without a date. Lists show upcoming events first (old undated ones before them), then past
  ones; past and cancelled events carry a badge, and the register button disappears automatically after the event ends, when
  it is cancelled, or when registration is closed. Events are listed on `/forum/events`; the first three also on the Forum page.
- **«الشركاء والرعاة»**: name in both languages, **partner logo** (image), **partner website** (`https://`, optional; opens
  with `rel="sponsored noopener"`), **group**, and **«shown in the home page partner strip»**. **Groups** (the card at the
  bottom of the screen) are the headings that organise `/forum/partners` (e.g. «main sponsors»); deleting a group **does
  not delete its partners**: they are listed after the groups. Hide a partner without deleting it with the «ظاهر/مخفي» badge.
- **Slug:** lower-case English letters, digits and hyphens only (`innovest-forum-2026`), up to 100 characters; some words
  are reserved (e.g. `admin`, `media`, `en`). Left empty when adding, it is built from the English title (`-2` is added on a
  clash), otherwise `news-<id>` or `event-<id>`. **Changing it later is safe:** the old address keeps redirecting (301) to
  the new one, and no other item may take an old address of an existing item. Existing news and events received
  `news-<id>` and `event-<id>` addresses automatically with this release.
- **English:** an item with no English title and summary shows the Arabic text on its English page with a notice, and that page is
  **kept out of search engines** (`noindex`) and out of the sitemap, with its canonical pointing at the Arabic page; it enters
  the index once the English title and summary are filled.
- Deleting a news item or event deletes its page, its image and its old addresses.
- Existing news, events and partners are **kept unchanged**, and every partner is shown on the home page after the release.

### Uploading images

An image field exists on news, events, partners («logo»), site pages (shown on the page's card and when it is shared) and the
founder's photo (General texts).
- **Types:** JPG, PNG or WebP only. No SVG, no GIF, nothing else, even if the extension is renamed: the file's content is
  checked, and the extension, the declared type and the content must agree.
- **Sizes:** up to **5 MB** per file; **16 to 8000 pixels** on each side and no more than **25 million pixels**. Animated images
  (APNG, animated WebP) are refused. A refused file shows a message under the field and nothing is saved.
- **What happens to the file:** it is rebuilt from the image parts only; location and camera data (EXIF), comments and any data
  after the end of the image are removed, while the orientation and colour profile are kept. The pixels are not changed and
  the file is not recompressed, so upload an image of a sensible size (its weight is the page weight). The original file
  name is never used: the file gets a random key and is served from `/media/<key>.<ext>`.
- **Replace and remove:** choose a new file under «استبدال الصورة» and save: the old file is deleted. To remove the image tick
  «حذف الصورة الحالية» and save. Files are deleted from the server after the save succeeds; if the save fails the new file is not kept.
- **Alt text:** the image description for screen readers, in Arabic and English; empty means the title is used.
- An image at least 1200 pixels wide is used at full size when the page is shared on social media; otherwise the page image,
  then the default image, is used.
- Do not upload images that contain private data: everything uploaded is public.

## 9b. Speakers, TV guests, episodes, opportunities and sponsorship packages

In the sidebar: group «منتدى إنوفست العربي» → **المتحدثون** (speakers); group «شبكة المستثمرين الملائكيين» → **الفرص الاستثمارية**
(opportunities); group «البرنامج التلفزيوني» → **الحلقات** (episodes), **الضيوف** (guests) and **باقات الرعاية** (sponsorship
packages). These screens fill the former «coming soon» pages `/forum/speakers`, `/tv/episodes`, `/tv/guests` and
`/angel-network/opportunities` (and the `/en` twins); every item also has its own page at its slug.

- **Common rules:** once a list has a published item the page stops being «empty» and is indexed automatically (section 5). While the
  list has no published item the page shows the «coming soon» text (edited in «صفحات الموقع») and stays `noindex`. Hidden items appear
  nowhere. The **slug** and image replace/remove work as for news (section 9): lower-case English letters, digits and hyphens; changing
  a slug keeps the old address redirecting (301). An item's English page is indexed only when its English texts exist; otherwise it
  shows the Arabic with a notice and is `noindex`. The «ظاهر/مخفي» badges hide an item without deleting it, and «Order»
  (lower first) sets the order.
- **Speakers and guests** (the same fields): name, job title, organisation, bio, LinkedIn and X links (optional, `https://`), the
  **portrait**, slug, search engines, order, visible/hidden. The English page is indexed when the English name is filled (and the
  English job title and bio when the Arabic ones exist). Deleting a speaker removes them from their events; deleting a guest
  removes them from their episodes. A profile **without a description** (no bio and no search-engine description) is kept out of search engines
  (`noindex`) and out of the sitemap in both languages, and the English page needs its own description; add a bio to have it indexed. A speaker's or
  guest's page lists only their **published** events and episodes.
- **Linking speakers to events:** the «الفعاليات» edit screen has a checkbox group «متحدثو الفعالية (يظهرون في صفحتها بهذا الترتيب)».
  They appear in the **speakers list order** ("Order", then oldest first), not in the order you ticked them. The event page shows only the published speakers; a speaker's page lists their events. A deactivated speaker stays in the list
  with a mark and is not shown to visitors.
- **Episodes:** title, summary, description, **season**, **episode number** and **air date** (all optional), **video link**,
  **thumbnail** (the video poster and the share image), **episode guests** (checkboxes from «الضيوف»; shown in the guests list order: "Order", then oldest first), slug,
  search engines, order. The list is ordered with the most recent air date first, undated episodes last.
  - **Video link:** `https://` only. A **YouTube** link (`youtube.com/watch?v=...`, `youtu.be/...`, `/embed/`, `/shorts/` or `/live/` on
    youtube.com, youtube-nocookie.com or youtu.be) is shown with a player that **contacts YouTube only when the visitor clicks it**
    (the youtube-nocookie domain is used); any other `https://` link shows a «watch the episode» button. With the field empty no player
    is shown.
- **Opportunities:** title, summary, details, **sector** (chosen from the «sectors we serve» list, or none), stage and funding sought
  (**two free texts**, e.g. «on request»; no figure appears that you did not type), **«open to interest»**, image, slug, search
  engines.
  - **Draft and publish:** a new opportunity is saved as a **draft** («مسودة» badge) and is not shown on the site (nor in the
    sitemap, and its address is a 404 for visitors) until you set it to **published**. A published opportunity that is not open shows
    a «closed» mark with no interest button. Open ones are listed first.
  - The «express interest» button on an opportunity opens the Contact page until a dedicated form is added.
  - **Deleting a sector** from «sectors we serve» that an opportunity uses is refused with a message («deactivate it instead»).
- **Sponsorship packages:** unit (TV program or forum), package name, **price** (free text such as «on request»), **benefits** (one
  per line), order, visible/hidden. TV packages appear on the «Sponsorship» page (`/tv/sponsorship`), forum packages on «Partners and
  sponsors» (`/forum/partners`).
- **Hubs and hiding:** hiding a page (e.g. «Episodes») from the menu is done in «صفحات الموقع» → «in menu» (section 3); it stays
  reachable by its URL. `/tv` and `/tv/sponsorship` stay `noindex` while their intro and main text are empty.
- **Images:** all of the above use the same upload tool and limits (JPG, PNG or WebP up to 5 MB, see «Uploading images»); an image is
  deleted with its item.

## 10. FAQ

«الأسئلة الشائعة»: question and answer in both languages, order, active. Shown on the home page and on Contact. When no
active question remains, the section and its link disappear from the page.

## 11. Interest options (register form list)

«خيارات مجال الاهتمام»: name in both languages, order, active. A registration stores the **Arabic option name**, so old
records do not change if you later edit or delete an option. A value that is not in the list is refused.

## 12. Register benefits

«مزايا التسجيل»: the points shown beside the register form. Text in both languages, order, active. With no active
point the box disappears.

## 13. Social links

«روابط التواصل الاجتماعي»: seven fixed networks (LinkedIn, X/Twitter, Instagram, YouTube, Facebook, TikTok, Linktree). Per
network: full `https://` URL, order, active. **A network with no URL is not shown** in the footer. Active links are also
added to the site's structured data (`sameAs`).

## 14. Applications inbox, registrations and contact messages

### «الطلبات (كل النماذج)» (`/Admin/Leads`)
Sidebar → «الوارد» → «الطلبات (كل النماذج)». One inbox for everything visitors send through the site's forms, **newest first, 50
per page** with paging. A badge with the **unread** count sits beside «الطلبات» and «رسائل التواصل» in the sidebar, and the
«نظرة عامة» dashboard has a card with the unread count per type.
- **Types** («النوع» filter): forum registration · join — investor · join — startup · sponsorship enquiry · interest in
  opportunities · quick registration (home page). **«القراءة» filter:** all / unread / read, and **«الحالة» filter:** all / new / contacted / closed. The three filters combine and are kept after you mark, save or
  delete a lead.
- **The table:** read state («جديد» or «مقروء»), a **status** badge (new / contacted / closed), a ✎ mark when a note exists, type, name, e-mail, details (organisation, type, event...), the language of the
  page the form was sent from, the date (in your browser's time zone; hover for UTC), and actions.
- **Details:** opens every field of the lead. The **read/unread** button toggles the status (a lead is not marked read just by
  opening it), and **delete** asks for confirmation; **deletion is permanent**.
- **Follow-up status and internal note** (on the details screen): choose the status — **new** (every lead on arrival and every old lead), then **contacted**, then **closed** — and type an **internal note** (up to 2000 characters, e.g. "called him on Monday"), then save. The status and the note are **never visible to visitors** and cannot be sent from the public forms; they are separate from read/unread. One button saves both.
- **Export to Excel (CSV):** the «تصدير إلى Excel (CSV)» link in the list header downloads `leads-<type or all>-<date>.csv` with **the leads that match the current filters** (type, read state, status), newest first, without paging. Columns: type, id, date (UTC), name, e-mail, phone, details, status, read, page language, internal note. The file is UTF-8 **with a BOM**, so Excel opens the Arabic correctly with a double-click, and every cell is quoted. **A value that starts with `=`, `+`, `-`, `@` or `|` (or their fullwidth forms ＝ ＋ － ＠, also after leading spaces or a tab) is prefixed with a `'`** so Excel never runs it as a formula; you will see the `'` at the start of such a cell (e.g. `'=SUM(1)`), which is intended. Dates are UTC text. The downloaded file is **personal data**: treat it like the database, and do not leave it in Downloads or send it over an insecure channel.
- What each form collects (the first four require name, e-mail and the consent box; the phone is optional):
  - **Forum registration** (`/forum/registration`): organisation, job title, country, area of interest (from «خيارات مجال
    الاهتمام»), and the event (optional; an active event only, otherwise «any event»).
  - **Join** (`/angel-network/join`): type (investor or startup) and country (required). Investor: organisation, investor type,
    ticket size, sectors, LinkedIn link (`https://`), message. Startup: **startup name, sector and stage are required**, plus
    funding sought, website (`https://`), pitch.
  - **Sponsorship** (`/tv/sponsorship`): company name, contact person, what to sponsor (the forum or the TV program), package
    (when packages exist), message.
  - **Opportunity interest** (`/angel-network/opportunities`): name, e-mail, phone, message; the opportunity reads «in
    general» while opportunity pages do not exist yet.
  - **Quick registration** on the home page is as before (name, e-mail, phone, area of interest).
  Each lead also stores the consent time, the page language and the submission time. Choice lists are stored with their Arabic
  text at submission time, so an old lead does not change if you later edit the options.
- **Editing the form texts and options:** «النصوص العامة» → card «نماذج الطلبات» (the consent text and the side headings and
  texts of the join, sponsorship and interest forms); «القوائم» → four lists: investor types, ticket sizes, startup stages,
  funding sought (sectors come from the «sectors we serve» list). A hidden option is not offered in the form.
- The «Opportunities» page shows the interest form **under its «coming soon» text** without counting as page content (it stays
  `noindex`). The «Sponsorship» page becomes indexable now that it has an intro paragraph.
- No e-mail is sent when a lead arrives: check the inbox (the badge) regularly.

### Personal data (PII)
Leads are personal data: **visible only to signed-in admins** and never on the public site. The application writes nothing from
the forms to its logs. The visitor's IP address and browser are not stored with the lead. Delete what you no longer need and do
not copy it to insecure channels. The CSV export is available to admins only (see above).

### «التسجيلات» and «رسائل التواصل»
«التسجيلات» lists every registration (with a «form» column: quick or forum) and «رسائل التواصل» the contact page's messages:
newest first, **50 per page**. The page language is shown. A message can be marked read or deleted; deletion is permanent. The
forms post to `/forms/...` (or `/en/forms/...`).

## 15. Changing the password

«الإعدادات»: enter the current password, the new one (10 to 64 characters) and its confirmation. Other sessions end;
yours stays. Failed attempts count: 5 failures on the account within 15 minutes lock it for 15 minutes.

## 16. Viewing the site

Bottom of the sidebar: «عرض الموقع» (`/`) and «عرض النسخة الإنجليزية» (`/en`). Check both languages after each edit.
The current logo is a raster image keyed from the two supplied logo files; the vector (SVG) logo has not been delivered yet.
