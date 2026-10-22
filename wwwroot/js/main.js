/* ==========================================
   INNOVEST HOLDING - Main JavaScript
   Animations, Interactions, and UI Logic
   ========================================== */

document.addEventListener('DOMContentLoaded', () => {
  // Direction comes from <html dir> (set by the server from the URL).
  // Page language and direction come from <html lang dir> (set by the server from the URL).
  // Latin digits in both languages (the company profile writes its figures as +100,000).
  const numberLocale = 'en-US';
  const reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const dirSign = document.documentElement.dir === 'ltr' ? -1 : 1;

  // ==========================================
  // 1. HEADER SCROLL BEHAVIOR
  // ==========================================
  const header = document.getElementById('main-header');
  if (!header) return; // pages without the site shell (the error page)
  let lastScrollY = 0;

  function handleHeaderScroll() {
    const scrollY = window.scrollY;

    if (scrollY > 50) {
      header.classList.add('scrolled');
    } else {
      header.classList.remove('scrolled');
    }

    lastScrollY = scrollY;
  }

  window.addEventListener('scroll', handleHeaderScroll, { passive: true });

  // ==========================================
  // 2. MOBILE MENU
  // ==========================================
  const hamburger = document.getElementById('hamburger-menu');
  const mobileNav = document.getElementById('mobile-nav-drawer');
  const mobileOverlay = document.getElementById('nav-overlay');
  const mobileDropdownTriggers = document.querySelectorAll('.mobile-dropdown-trigger');

  // The sub-list a drawer trigger controls (aria-controls), e.g. «Our Ecosystem» → the three units → their pages.
  const listOf = (trigger) => document.getElementById(trigger.getAttribute('aria-controls') || '');

  // Collapses every sub-list of the drawer (aria-expanded follows).
  function collapseMobileDropdowns() {
    mobileDropdownTriggers.forEach(trigger => {
      const list = listOf(trigger);
      if (list) list.classList.remove('open');
      trigger.setAttribute('aria-expanded', 'false');
    });
  }

  // Every way of closing the drawer (hamburger, overlay, Escape, a link) goes through here, so the drawer always
  // reopens with its sub-lists collapsed.
  function toggleMobileMenu() {
    hamburger.classList.toggle('active');
    const open = mobileNav.classList.toggle('open');
    mobileOverlay.classList.toggle('open');
    hamburger.setAttribute('aria-expanded', open ? 'true' : 'false');
    document.body.style.overflow = open ? 'hidden' : '';
    // Opening moves focus to the drawer's first control (pages.css keeps the drawer's controls from transitioning
    // their visibility, which would leave them unfocusable for the first frame); closing returns it to the menu button.
    if (open) {
      const first = mobileNav.querySelector('a, button');
      if (first) first.focus();
    } else {
      collapseMobileDropdowns();
      hamburger.focus();
    }
  }

  // The hamburger is a <button>: a click (or Enter/Space) toggles the drawer.
  if (hamburger && mobileNav) {
    hamburger.addEventListener('click', toggleMobileMenu);
  }

  // Focus stays inside the open drawer (aria-modal): Tab from the last control returns to the first and back.
  if (mobileNav) {
    mobileNav.addEventListener('keydown', (e) => {
      if (e.key !== 'Tab' || !mobileNav.classList.contains('open')) return;
      const focusable = Array.from(mobileNav.querySelectorAll('a[href], button')).filter(el => el.offsetParent !== null);
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    });
  }

  if (mobileOverlay) {
    mobileOverlay.addEventListener('click', toggleMobileMenu);
  }

  // Mobile dropdown toggle
  // The trigger only opens/closes its sub-list: it neither scrolls nor closes the drawer (the section link is one
  // of the sub-items' targets anyway). aria-expanded carries the state for assistive technology and the arrow.
  mobileDropdownTriggers.forEach(trigger => {
    trigger.setAttribute('aria-expanded', 'false');
    trigger.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopImmediatePropagation();
      const dropdownItems = listOf(trigger);
      if (dropdownItems) {
        const open = dropdownItems.classList.toggle('open');
        trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
      }
    });
  });

  // Close mobile menu on nav link click (including dropdown items)
  document.querySelectorAll('.mobile-nav .nav-links a').forEach(link => {
    link.addEventListener('click', () => {
      if (mobileNav && mobileNav.classList.contains('open')) {
        toggleMobileMenu();
      }
    });
  });

  // Close mobile menu on Escape key
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && mobileNav && mobileNav.classList.contains('open')) {
      toggleMobileMenu();
    }
  });

  // Desktop dropdown («منصاتنا»): CSS opens it on hover and on keyboard focus (:focus-within). The trigger's
  // aria-expanded mirrors that, and Escape closes it and puts focus back on the trigger. The "is-dismissed" class keeps
  // it closed while focus stays on the trigger; leaving the dropdown, or moving into it again, clears it.
  document.querySelectorAll('header .nav-dropdown').forEach(dropdown => {
    const trigger = dropdown.querySelector(':scope > .nav-link');
    if (!trigger) return;
    const setExpanded = (value) => trigger.setAttribute('aria-expanded', value ? 'true' : 'false');
    const reopen = () => { dropdown.classList.remove('is-dismissed'); setExpanded(true); };

    dropdown.addEventListener('mouseenter', reopen);
    dropdown.addEventListener('mouseleave', () => {
      // Still focused after Escape: stays dismissed (otherwise :focus-within would reopen it as the pointer leaves).
      if (dropdown.classList.contains('is-dismissed')) return;
      setExpanded(dropdown.contains(document.activeElement));
    });
    dropdown.addEventListener('focusin', (e) => {
      // Focus arriving on the trigger after Escape must not reopen it; focus on an item (Tab) does.
      if (e.target === trigger && dropdown.classList.contains('is-dismissed')) return;
      reopen();
    });
    dropdown.addEventListener('focusout', (e) => {
      if (!dropdown.contains(e.relatedTarget)) {
        dropdown.classList.remove('is-dismissed');
        setExpanded(dropdown.matches(':hover'));
      }
    });
    // On the document, so Escape also closes a menu that only the pointer opened (focus is then elsewhere).
    document.addEventListener('keydown', (e) => {
      if (e.key !== 'Escape' || dropdown.classList.contains('is-dismissed')) return;
      if (!dropdown.contains(document.activeElement) && !dropdown.matches(':hover')) return;
      dropdown.classList.add('is-dismissed');
      setExpanded(false);
      trigger.focus();
    });
  });

  // ==========================================
  // 3. SMOOTH SCROLL FOR ANCHOR LINKS
  // ==========================================
  document.querySelectorAll('a[href^="#"]:not(.mobile-dropdown-trigger)').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      const targetId = this.getAttribute('href');
      if (targetId === '#') return;

      e.preventDefault();
      const target = document.querySelector(targetId);
      if (target) {
        const headerOffset = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--header-height')) || 80;
        const elementPosition = target.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.scrollY - headerOffset;

        window.scrollTo({
          top: offsetPosition,
          behavior: 'smooth'
        });

        // Close mobile menu if open
        if (mobileNav && mobileNav.classList.contains('open')) {
          toggleMobileMenu();
        }
      }
    });
  });

  // ==========================================
  // 4. SCROLL REVEAL ANIMATIONS
  // ==========================================
  const revealElements = document.querySelectorAll('.reveal, .reveal-start, .reveal-end');

  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('active');
        revealObserver.unobserve(entry.target);
      }
    });
  }, {
    threshold: 0.12,
    rootMargin: '0px 0px -60px 0px'
  });

  revealElements.forEach(el => revealObserver.observe(el));

  // ==========================================
  // 5. ANIMATED COUNTERS
  // ==========================================
  const counters = document.querySelectorAll('[data-counter]');

  function animateCounter(el) {
    const target = parseInt(el.getAttribute('data-counter'));
    const suffix = el.getAttribute('data-suffix') || '';
    const prefix = el.getAttribute('data-prefix') || '';
    const duration = 2000;
    const startTime = performance.now();

    function update(currentTime) {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);

      // Ease out cubic
      const eased = 1 - Math.pow(1 - progress, 3);
      const current = Math.floor(eased * target);

      el.textContent = prefix + current.toLocaleString(numberLocale) + suffix;

      if (progress < 1) {
        requestAnimationFrame(update);
      } else {
        el.textContent = prefix + target.toLocaleString(numberLocale) + suffix;
      }
    }

    requestAnimationFrame(update);
  }

  const counterObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        animateCounter(entry.target);
        counterObserver.unobserve(entry.target);
      }
    });
  }, {
    threshold: 0.5
  });

  // The server renders the final figure; the count-up runs only when motion is welcome.
  if (!reducedMotion) counters.forEach(counter => counterObserver.observe(counter));

  // ==========================================
  // 6. FAQ ACCORDION
  // ==========================================
  // An open item carries "is-open". It must not use "active": that class is the scroll-reveal state of the same
  // element (.reveal.active = visible), so closing the items would also hide them.
  const faqItems = document.querySelectorAll('.faq-item');

  faqItems.forEach(item => {
    const question = item.querySelector('.faq-question');
    const answer = item.querySelector('.faq-answer');
    const answerInner = item.querySelector('.faq-answer-inner');

    question.addEventListener('click', () => {
      const isOpen = item.classList.contains('is-open');

      // Close all
      faqItems.forEach(otherItem => {
        otherItem.classList.remove('is-open');
        const otherAnswer = otherItem.querySelector('.faq-answer');
        if (otherAnswer) otherAnswer.style.maxHeight = '0';
        const otherQuestion = otherItem.querySelector('.faq-question');
        if (otherQuestion) otherQuestion.setAttribute('aria-expanded', 'false');
      });

      // Toggle current
      if (!isOpen) {
        item.classList.add('is-open');
        answer.style.maxHeight = answerInner.scrollHeight + 20 + 'px';
        question.setAttribute('aria-expanded', 'true');
      }
    });
  });

  // ==========================================
  // 7. HERO PARALLAX EFFECT
  // ==========================================
  const heroSection = document.querySelector('.hero-section');
  const heroPattern = document.querySelector('.hero-pattern');
  const heroGlow = document.querySelector('.hero-glow');

  if (heroSection && heroPattern) {
    window.addEventListener('scroll', () => {
      const scrolled = window.scrollY;
      const heroHeight = heroSection.offsetHeight;

      if (scrolled < heroHeight) {
        const parallaxSpeed = 0.3;
        heroPattern.style.transform = `translateY(${scrolled * parallaxSpeed}px)`;

        if (heroGlow) {
          heroGlow.style.transform = `scale(${1 + scrolled * 0.0005}) translate(${scrolled * 0.05 * dirSign}px, ${scrolled * 0.02}px)`;
        }
      }
    }, { passive: true });
  }

  // ==========================================
  // 8. ACTIVE NAV LINK ON SCROLL
  // ==========================================
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.nav-link[href^="#"]');

  // The language switch is a plain link to the other language (works without JS); here it also carries the
  // section the visitor is reading, e.g. /en#about.
  const langLinks = document.querySelectorAll('#lang-switch, .mobile-lang-link');
  langLinks.forEach(link => { link.dataset.base = link.getAttribute('href').split('#')[0]; });
  function updateLangLinks(sectionId) {
    langLinks.forEach(link => {
      link.setAttribute('href', link.dataset.base + (sectionId && sectionId !== 'hero' ? '#' + sectionId : ''));
    });
  }

  function setActiveNav() {
    const scrollY = window.scrollY;
    const headerHeight = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--header-height')) || 80;

    sections.forEach(section => {
      const sectionTop = section.offsetTop - headerHeight - 100;
      const sectionBottom = sectionTop + section.offsetHeight;
      const sectionId = section.getAttribute('id');

      if (scrollY >= sectionTop && scrollY < sectionBottom) {
        navLinks.forEach(link => {
          link.classList.remove('active');
          if (link.getAttribute('href') === `#${sectionId}`) {
            link.classList.add('active');
          }
        });
        updateLangLinks(sectionId);
      }
    });
  }

  window.addEventListener('scroll', setActiveNav, { passive: true });

  // ==========================================
  // 9. FORM HANDLING — register / contact (fetch, JSON reply)
  // ==========================================
  // Texts come from the page (data-msg-busy, data-msg-error, rendered from the resx in the page language); the
  // server's JSON message is already in the page language. A non-JSON reply (e.g. an HTML error page) is an error.
  function bindAjaxForm(form) {
    const msg = document.getElementById(form.dataset.messageBox);
    const btn = form.querySelector('button[type="submit"]');
    const label = btn ? btn.querySelector('span') : null;
    const idleText = label ? label.textContent : '';

    function show(ok, text) {
      msg.hidden = false;
      msg.classList.toggle('is-ok', ok);
      msg.classList.toggle('is-error', !ok);
      msg.textContent = text;
    }
    function idle() {
      btn.disabled = false;
      if (label) label.textContent = idleText;
    }

    form.addEventListener('submit', (e) => {
      e.preventDefault();
      btn.disabled = true;
      if (label) label.textContent = form.dataset.msgBusy;

      fetch(form.action, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams(new FormData(form))
      })
        .then(r => {
          const type = r.headers.get('content-type') || '';
          if (type.indexOf('application/json') === -1) throw new Error('non-json');
          return r.json();
        })
        .then(data => {
          show(!!data.success, data.message || form.dataset.msgError);
          if (data.success) form.reset();
          idle();
          setTimeout(() => { msg.hidden = true; }, 5000);
        })
        .catch(() => {
          show(false, form.dataset.msgError);
          idle();
        });
    });
  }
  document.querySelectorAll('form.js-ajax-form').forEach(bindAjaxForm);

  // Unit tab bar: on a narrow screen the current tab may sit outside the scrollable strip — bring it into view
  // (only the strip scrolls; the page does not move).
  const activeTab = document.querySelector('.unit-tabs a.active');
  const tabStrip = document.querySelector('.unit-tabs-inner');
  if (activeTab && tabStrip && tabStrip.scrollWidth > tabStrip.clientWidth) {
    const strip = tabStrip.getBoundingClientRect();
    const tab = activeTab.getBoundingClientRect();
    tabStrip.scrollLeft += (tab.left + tab.width / 2) - (strip.left + strip.width / 2);
  }

  // ==========================================
  // 10. TYPING EFFECT FOR HERO (optional enhancement)
  // ==========================================
  // Not used by default, can be enabled if needed

  // ==========================================
  // UTILITY: Add CSS animation keyframe for spinner
  // ==========================================
  const styleSheet = document.createElement('style');
  styleSheet.textContent = `
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `;
  document.head.appendChild(styleSheet);

});
