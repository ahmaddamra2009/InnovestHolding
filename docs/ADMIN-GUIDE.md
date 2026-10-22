# Admin guide / دليل لوحة التحكم

العربية أولاً، ثم English. / Arabic first, then English.

---

# العربية

لوحة التحكم عربية. كل نص في الموقع له قيمتان: عربية (تظهر في `/`) وإنجليزية (تظهر في `/en`). الحقل العربي مطلوب
في أغلب الأحوال، والحقل الإنجليزي اختياري. بجانب كل حقل شارة «ع» أو «EN» لتعرف أي لغة تكتب.

## 1. قواعد عامة

- **الاحتياط (fallback):** إذا تُرك الحقل الإنجليزي فارغاً، تعرض الصفحة الإنجليزية النص العربي بدلاً منه (ويُعرض
  من اليمين إلى اليسار). الصفحة العربية لا تعرض الإنجليزي أبداً. بعض الحقول لا احتياط لها (مثل السطر العلوي في
  Hero): تبقى فارغة في الصفحة الإنجليزية إن تُرك حقلها الإنجليزي فارغاً.
- **يظهر التغيير فوراً:** بعد الحفظ يظهر في الصفحتين العربية والإنجليزية عند الطلب التالي. لا حاجة لإعادة تشغيل.
- **الإنجليزية الحالية مسودة** وتحتاج مراجعة قبل اعتمادها؛ عدّلوها من الشاشات أدناه.
- **الحقول المحايدة** (الهاتف، البريد، الروابط) لها قيمة واحدة للغتين وتُكتب من اليسار إلى اليمين.
- **الأوقات** في «التسجيلات» و«رسائل التواصل» تُحفظ بتوقيت UTC وتُعرض بتوقيت **المتصفح الخاص بمن يفتح الصفحة**
  وبلغة الصفحة. إن كان JavaScript معطّلاً تظهر القيمة بتوقيت UTC مع كلمة UTC. مرّر المؤشر على الوقت لترى قيمة UTC.
- **الروابط:** روابط التواصل الاجتماعي ورابط الجهة المطوّرة يجب أن تبدأ بـ `https://`. روابط القائمة والمنصات
  والأخبار تقبل `#قسم` أو `/مسار` أو `https://…`، وتُرفض `javascript:` وما شابهه. اتركوا الحقل فارغاً إن لم يوجد رابط.
- لا تكتبوا كلمات مرور أو بيانات سرية في أي حقل: كل ما يُكتب في المحتوى يظهر للعامة.

## 2. الصفحة الرئيسية (تعديل المحتوى)

القائمة الجانبية ← «الصفحة الرئيسية». الشاشة بطاقات: الهيدر وزر التسجيل، Hero، من نحن، عناوين الأقسام، قسم
التسجيل، معلومات التواصل، الفوتر، SEO وبيانات الموقع. لكل نص حقل عربي وحقل إنجليزي ثم «حفظ».

- **الجزء المميز في العناوين:** ضعوا الجزء الذي يظهر بخط ذهبي بين `{` و`}`. يجب أن يحتوي النص على زوج واحد فقط
  من الأقواس، وإلا يُرفض الحفظ.
- **سنوات الخبرة، الهاتف، البريد:** قيمة واحدة للغتين.
- **نص شارة الخبرة:** كل سطر يظهر في سطر.
- **حقوق النشر:** اكتبوا `{year}` ليُستبدل بالسنة الحالية.
- **رابط الجهة المطوّرة** (الفوتر): `https://` أو فارغ.
- **السطر العلوي في Hero:** اختياري، ولا يظهر في الصفحة الإنجليزية إن تُرك حقله الإنجليزي فارغاً.
- **حفظ الشاشة لا يغيّر ما لم يُرسل:** أي مفتاح غير موجود في النموذج يبقى كما هو.

### رابط الموقع الأساسي (بطاقة SEO وبيانات الموقع)
الحقل «رابط الموقع الأساسي»: `https://النطاق` فقط، **بدون مسار ولا شرطة أخيرة** ولا `?` ولا `#` (القيمة المبذورة
`https://www.innovest-holding.com`). منه تُبنى وسوم canonical وhreflang و`og:url` وملف `/sitemap.xml` وسطر
`Sitemap:` في `/robots.txt`. إذا تُرك فارغاً في الإنتاج تختفي هذه الوسوم ويعيد `/sitemap.xml` الخطأ 404.
«صورة المشاركة» مسار داخل الموقع يبدأ بـ `/` (مثل `/assets/logo.png`).

## 3. القائمة الرئيسية

«القائمة الرئيسية». لكل عنصر: الاسم بالعربية والإنجليزية، الرابط، «مفعّل» (يخفي العنصر دون حذفه)، و«يظهر في
«روابط سريعة» بالفوتر». العناصر الفرعية تُضاف وتُعدَّل (الاسم بالعربية والإنجليزية والرابط) وتُحذف من نفس الشاشة.
الترتيب هو ترتيب الإدخال الحالي ولا يغيّره هذا الإصدار.

## 4. المنصات

«المنصات» (تعديل المنصات الموجودة): العنوان والوصف بالعربية والإنجليزية، **العنوان المختصر** (يُستعمل في الفوتر،
وإن تُرك فارغاً يُستعمل العنوان الكامل)، **الرابط** لزر «اعرف المزيد» (إن تُرك فارغاً **لا يظهر الزر**)، و«يظهر في عمود
«منصاتنا» بالفوتر».

## 5. الإحصائيات

«الإحصائيات»: التسمية بالعربية والإنجليزية، التسمية المختصرة، الرقم، واللاحقة (مثل `+`).

## 6. الأخبار

«الأخبار»: إضافة وتعديل وحذف. العنوان والملخص والتصنيف والتاريخ بالعربية والإنجليزية (**التاريخ نص حر** وليس
تاريخاً فعلياً). **الرابط** لزر «اقرأ المزيد»: إن تُرك فارغاً لا يظهر الزر. «الترتيب»: الرقم الأصغر أولاً. «مفعّل» يخفي
الخبر دون حذفه. الخبر الجديد يظهر أولاً. **عدد الأخبار في الصفحة الرئيسية:** من «الصفحة الرئيسية» ← بطاقة «عناوين الأقسام» ← «الأخبار — عدد الأخبار المعروضة في الصفحة الرئيسية»، رقم صحيح من 1 إلى 50 (الافتراضي 6)؛ تظهر الصفحة أول هذا العدد من الأخبار المفعّلة، وإن كانت القيمة غير صالحة يُستعمل 6.

## 7. الفعاليات

«الفعاليات»: العنوان والوصف والشهر والمكان والوقت بالعربية والإنجليزية (**نصوص حرة**)، رقم اليوم (1–31)، «الترتيب»،
و«مفعّل».

## 8. الشركاء

«الشركاء»: إضافة وتعديل (الاسم بالعربية والإنجليزية، من «تعديل») وحذف.

## 9. الأسئلة الشائعة

«الأسئلة الشائعة»: السؤال والجواب بالعربية والإنجليزية، «الترتيب»، «مفعّل». إذا لم يبقَ سؤال مفعّل يختفي القسم
ورابطه من الصفحة.

## 10. خيارات مجال الاهتمام (قائمة نموذج التسجيل)

«خيارات مجال الاهتمام»: الاسم بالعربية والإنجليزية، الترتيب، مفعّل. عند التسجيل يُحفظ **اسم الخيار بالعربية**
مع التسجيل، فلا يتغير سجل قديم إن عدّلتم الخيار أو حذفتموه لاحقاً. القيمة غير الموجودة في القائمة تُرفض.

## 11. مزايا التسجيل

«مزايا التسجيل»: النقاط التي تظهر بجانب نموذج التسجيل. النص بالعربية والإنجليزية، الترتيب، مفعّل. إن لم تبقَ نقطة
مفعّلة يختفي الصندوق.

## 12. روابط التواصل الاجتماعي

«روابط التواصل الاجتماعي»: أربع شبكات ثابتة (LinkedIn وX/Twitter وInstagram وYouTube). لكل شبكة: الرابط
(`https://` كامل)، الترتيب، مفعّل. **الشبكة بلا رابط لا تظهر** في الفوتر. تُضاف الروابط المفعّلة أيضاً إلى البيانات
المنظمة (`sameAs`) للموقع.

## 13. التسجيلات ورسائل التواصل

«التسجيلات» و«رسائل التواصل»: الأحدث أولاً، **50 في الصفحة** مع تنقل بين الصفحات. تظهر لغة الصفحة التي أُرسل منها
النموذج (عربي/إنجليزي). يمكن تعليم الرسالة «مقروء» أو حذفها، والحذف نهائي.

## 14. تغيير كلمة المرور

«الإعدادات»: أدخلوا كلمة المرور الحالية ثم الجديدة (من 10 إلى 64 حرفاً) وتأكيدها. تنتهي بقية الجلسات وتبقى جلستكم.
المحاولات الخاطئة تُحتسب: 5 محاولات فاشلة على الحساب خلال 15 دقيقة تقفله 15 دقيقة.

## 15. عرض الموقع

أسفل القائمة الجانبية: «عرض الموقع» (`/`) و«عرض النسخة الإنجليزية» (`/en`). راجعوا الصفحتين بعد كل تعديل.

---

# English

The panel itself is in Arabic. Every public text has two values: Arabic (shown on `/`) and English (shown on `/en`).
The Arabic field is usually required, the English field optional. Each field carries an «ع» or «EN» chip so you know
which language you are typing.

## 1. General rules

- **Fallback:** if an English field is left empty, the English page shows the Arabic text instead (displayed
  right-to-left). The Arabic page never shows English. A few fields have no fallback (e.g. the Hero kicker): they stay
  empty on the English page when their English field is empty.
- **Changes are live immediately:** after saving, both pages show the change on the next request. No restart needed.
- **The current English text is a draft** pending review; edit it from the screens below.
- **Neutral fields** (phone, e-mail, URLs) have one value for both languages and are typed left-to-right.
- **Times** in «التسجيلات» and «رسائل التواصل» are stored in UTC and shown in **the time zone of the viewer's
  browser**, in the page's language. With JavaScript off the UTC value is shown, labelled UTC. Hover over a time to
  see its UTC value.
- **Links:** social-media links and the developer-credit link must start with `https://`. Menu, platform and news
  links accept `#section`, `/path` or `https://…`; `javascript:` and similar are refused. Leave the field empty when
  there is no link.
- Never type passwords or secrets into any field: whatever is entered as content is public.

## 2. Home page (content editor)

Sidebar, «الصفحة الرئيسية». Cards: header and register button, Hero, About, section headings, register section,
contact details, footer, SEO and site data. Each text has an Arabic and an English field, then Save.

- **Highlighted part of a heading:** put the part shown in gold between `{` and `}`. Use exactly one pair of braces
  or the save is refused.
- **Years of experience, phone, e-mail:** one value for both languages.
- **Experience badge text:** each line is shown on its own line.
- **Copyright:** type `{year}` to insert the current year.
- **Developer credit link** (footer): `https://` or empty.
- **Hero kicker:** optional; hidden on the English page when its English field is empty.
- **Saving does not change what is not posted:** any key missing from the form stays as it was.

### Base URL (SEO and site data card)
The field «رابط الموقع الأساسي» takes `https://domain` only, **no path, no trailing slash**, no `?` and no `#` (seeded
value `https://www.innovest-holding.com`). Canonical, hreflang, `og:url`, `/sitemap.xml` and the `Sitemap:` line of
`/robots.txt` are built from it. If it is emptied in production those tags disappear and `/sitemap.xml` returns 404.
«Share image» is a site path starting with `/` (e.g. `/assets/logo.png`).

## 3. Main menu

«القائمة الرئيسية». Per item: name in Arabic and English, link, «active» (hides the item without deleting it) and
«shown in footer quick links». Sub-items are added, edited (name in both languages and link) and deleted on the same
screen. The order is the current insertion order; this release does not change it.

## 4. Platforms

«المنصات» (edits the existing platforms): title and description in both languages, a **short title** (used in the
footer; the full title is used when it is empty), the **link** of the «اعرف المزيد» button (**the button is hidden when
the link is empty**), and «shown in the footer platforms column».

## 5. Statistics

«الإحصائيات»: label in both languages, short label, number and suffix (e.g. `+`).

## 6. News

«الأخبار»: add, edit, delete. Title, summary, category and date in both languages (**the date is free text**, not a real
date). **Link** of the «اقرأ المزيد» button: hidden when empty. «Order»: lower number first. «Active» hides an item
without deleting it. A new item appears first. **Number of news items on the home page:** «الصفحة الرئيسية» card «عناوين الأقسام», field «الأخبار — عدد الأخبار المعروضة في الصفحة الرئيسية»: a whole number from 1 to 50 (default 6); the page shows the first that many active items, and an invalid stored value falls back to 6.

## 7. Events

«الفعاليات»: title, description, month, place and time in both languages (**all free text**), day number (1-31),
order and active.

## 8. Partners

«الشركاء»: add, edit (name in both languages, under «تعديل») and delete.

## 9. FAQ

«الأسئلة الشائعة»: question and answer in both languages, order, active. When no active question remains, the section
and its link disappear from the page.

## 10. Interest options (register form list)

«خيارات مجال الاهتمام»: name in both languages, order, active. A registration stores the **Arabic option name**, so old
records do not change if you later edit or delete an option. A value that is not in the list is refused.

## 11. Register benefits

«مزايا التسجيل»: the points shown beside the register form. Text in both languages, order, active. With no active
point the box disappears.

## 12. Social links

«روابط التواصل الاجتماعي»: four fixed networks (LinkedIn, X/Twitter, Instagram, YouTube). Per network: full
`https://` URL, order, active. **A network with no URL is not shown** in the footer. Active links are also added to
the site's structured data (`sameAs`).

## 13. Registrations and contact messages

«التسجيلات» and «رسائل التواصل»: newest first, **50 per page** with paging. The language of the page the form was sent
from (Arabic/English) is shown. A message can be marked read or deleted; deletion is permanent.

## 14. Changing the password

«الإعدادات»: enter the current password, the new one (10 to 64 characters) and its confirmation. Other sessions end;
yours stays. Failed attempts count: 5 failures on the account within 15 minutes lock it for 15 minutes.

## 15. Viewing the site

Bottom of the sidebar: «عرض الموقع» (`/`) and «عرض النسخة الإنجليزية» (`/en`). Check both pages after each edit.
