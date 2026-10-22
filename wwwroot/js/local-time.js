/* Shows every <time class="utc-time" datetime="…Z"> in the viewer's own time zone (from the browser) and in the
   page language (<html lang>). The server renders the UTC value with a "UTC" label, which stays when JS is off. */
(function () {
  var lang = (document.documentElement.lang || 'ar').toLowerCase();
  var locale = (lang === 'en' ? 'en-GB' : 'ar') + '-u-nu-latn-ca-gregory';
  var format;
  try {
    format = new Intl.DateTimeFormat(locale, {
      year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: true
    });
  } catch (e) {
    return; // no Intl: keep the UTC text
  }
  document.querySelectorAll('time.utc-time[datetime]').forEach(function (el) {
    var d = new Date(el.getAttribute('datetime'));
    if (isNaN(d.getTime())) return;
    el.title = el.textContent;          // the UTC value stays available on hover
    el.textContent = format.format(d);
  });
})();
