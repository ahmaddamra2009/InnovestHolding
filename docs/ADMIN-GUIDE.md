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

## 9. الأخبار والفعاليات والشركاء

- **«الأخبار»**: إضافة وتعديل وحذف. العنوان والملخص والتصنيف والتاريخ بالعربية والإنجليزية (**التاريخ نص حر** وليس
  تاريخاً فعلياً). **الرابط** لزر «اقرأ المزيد»: إن تُرك فارغاً لا يظهر الزر. «الترتيب»: الرقم الأصغر أولاً. «مفعّل»
  يخفي الخبر دون حذفه. الخبر الجديد يظهر أولاً. تظهر الأخبار في صفحة `/news` مرقّمة، وفي الرئيسية أول **عدد الأخبار المعروضة
  في الصفحة الرئيسية** (النصوص العامة ← «عناوين أقسام الصفحة الرئيسية»، رقم من 1 إلى 50، الافتراضي 6).
- **«الفعاليات»** (ضمن «منتدى إنوفست العربي»): العنوان والوصف والشهر والمكان والوقت بالعربية والإنجليزية (**نصوص حرة**)،
  رقم اليوم (1–31)، «الترتيب»، و«مفعّل». تظهر في `/forum/events`، وأول ثلاث منها في صفحة المنتدى.
- **«الشركاء والرعاة»**: إضافة وتعديل (الاسم بالعربية والإنجليزية، من «تعديل») وحذف، وشارة «ظاهر/مخفي» للإخفاء دون حذف.
  تظهر في `/forum/partners` وفي الرئيسية.
- الأخبار والفعاليات والشركاء الحالية **محفوظة كما هي** بعد هذا الإصدار.

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

## 14. التسجيلات ورسائل التواصل

«التسجيلات» و«رسائل التواصل»: الأحدث أولاً، **50 في الصفحة** مع تنقل بين الصفحات. تظهر لغة الصفحة التي أُرسل منها
النموذج (عربي/إنجليزي). يمكن تعليم الرسالة «مقروء» أو حذفها، والحذف نهائي. النموذجان يُرسَلان الآن إلى `/forms/register`
و`/forms/contact` (أو `/en/forms/…`)؛ لا أثر لذلك على ما تراه في اللوحة.

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

## 9. News, events and partners

- **«الأخبار»**: add, edit, delete. Title, summary, category and date in both languages (**the date is free text**, not a
  real date). **Link** of the «اقرأ المزيد» button: hidden when empty. «Order»: lower number first. «Active» hides an item
  without deleting it. A new item appears first. News is listed on `/news` with paging, and the home page shows the first
  **number of news items on the home page** (General texts → «عناوين أقسام الصفحة الرئيسية», a whole number from 1 to 50,
  default 6).
- **«الفعاليات»** (under «منتدى إنوفست العربي»): title, description, month, place and time in both languages (**all free
  text**), day number (1-31), order and active. Listed on `/forum/events`; the first three also on the Forum page.
- **«الشركاء والرعاة»**: add, edit (name in both languages, under «تعديل») and delete, plus a «ظاهر/مخفي» badge to hide
  without deleting. Shown on `/forum/partners` and on the home page.
- Existing news, events and partners are **kept unchanged** by this release.

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

## 14. Registrations and contact messages

«التسجيلات» and «رسائل التواصل»: newest first, **50 per page** with paging. The language of the page the form was sent
from (Arabic/English) is shown. A message can be marked read or deleted; deletion is permanent. The forms now post to
`/forms/register` and `/forms/contact` (or `/en/forms/…`); this changes nothing you see in the panel.

## 15. Changing the password

«الإعدادات»: enter the current password, the new one (10 to 64 characters) and its confirmation. Other sessions end;
yours stays. Failed attempts count: 5 failures on the account within 15 minutes lock it for 15 minutes.

## 16. Viewing the site

Bottom of the sidebar: «عرض الموقع» (`/`) and «عرض النسخة الإنجليزية» (`/en`). Check both languages after each edit.
The current logo is a raster image keyed from the two supplied logo files; the vector (SVG) logo has not been delivered yet.
