/* ==========================================
   INNOVEST HOLDING - Main JavaScript
   Animations, Interactions, and UI Logic
   ========================================== */

document.addEventListener('DOMContentLoaded', () => {

  // Page language and direction come from <html lang dir> (set by the server from the URL).
  const pageLang = (document.documentElement.lang || 'ar').toLowerCase();
  const numberLocale = pageLang === 'en' ? 'en-US' : 'ar-SA';
  const dirSign = document.documentElement.dir === 'ltr' ? -1 : 1;
  
  // ==========================================
  // 1. HEADER SCROLL BEHAVIOR
  // ==========================================
  const header = document.getElementById('main-header');
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
  
  // Collapses every sub-list of the drawer (aria-expanded follows).
  function collapseMobileDropdowns() {
    mobileDropdownTriggers.forEach(trigger => {
      if (trigger.nextElementSibling) trigger.nextElementSibling.classList.remove('open');
      trigger.setAttribute('aria-expanded', 'false');
    });
  }

  // Every way of closing the drawer (hamburger, overlay, Escape, a link) goes through here, so the drawer always
  // reopens with its sub-lists collapsed.
  function toggleMobileMenu() {
    hamburger.classList.toggle('active');
    const open = mobileNav.classList.toggle('open');
    mobileOverlay.classList.toggle('open');
    document.body.style.overflow = open ? 'hidden' : '';
    if (!open) collapseMobileDropdowns();
  }
  
  if (hamburger) {
    hamburger.addEventListener('click', toggleMobileMenu);
    hamburger.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleMobileMenu(); }
    });
  }
  
  if (mobileOverlay) {
    mobileOverlay.addEventListener('click', toggleMobileMenu);
  }
  
  // Mobile dropdown toggle
  // The trigger only opens/closes its sub-list: it neither scrolls nor closes the drawer (the section link is one
  // of the sub-items' targets anyway). aria-expanded carries the state for assistive technology and the arrow.
  mobileDropdownTriggers.forEach(trigger => {
    const dropdownItems = trigger.nextElementSibling;
    trigger.setAttribute('role', 'button');
    trigger.setAttribute('aria-expanded', 'false');
    if (dropdownItems && dropdownItems.id) trigger.setAttribute('aria-controls', dropdownItems.id);
    trigger.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopImmediatePropagation();
      if (dropdownItems) {
        const open = dropdownItems.classList.toggle('open');
        trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
      }
    });
    // role="button": Space activates it too (Enter already does, as a link).
    trigger.addEventListener('keydown', (e) => {
      if (e.key === ' ') { e.preventDefault(); trigger.click(); }
    });
  });
  
  // Close mobile menu on nav link click (including dropdown items)
  document.querySelectorAll('.mobile-nav .nav-link:not(.mobile-dropdown-trigger), .mobile-dropdown-items a').forEach(link => {
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
  
  counters.forEach(counter => counterObserver.observe(counter));
  
  // ==========================================
  // 6. FAQ ACCORDION
  // ==========================================
  const faqItems = document.querySelectorAll('.faq-item');
  
  faqItems.forEach(item => {
    const question = item.querySelector('.faq-question');
    const answer = item.querySelector('.faq-answer');
    const answerInner = item.querySelector('.faq-answer-inner');
    
    question.addEventListener('click', () => {
      const isActive = item.classList.contains('active');
      
      // Close all
      faqItems.forEach(otherItem => {
        otherItem.classList.remove('active');
        const otherAnswer = otherItem.querySelector('.faq-answer');
        if (otherAnswer) otherAnswer.style.maxHeight = '0';
      });
      
      // Toggle current
      if (!isActive) {
        item.classList.add('active');
        answer.style.maxHeight = answerInner.scrollHeight + 20 + 'px';
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
      msg.style.display = 'block';
      msg.style.background = ok ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)';
      msg.style.color = ok ? '#16a34a' : '#ef4444';
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
          setTimeout(() => { msg.style.display = 'none'; }, 5000);
        })
        .catch(() => {
          show(false, form.dataset.msgError);
          idle();
        });
    });
  }
  document.querySelectorAll('form.js-ajax-form').forEach(bindAjaxForm);
  
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
