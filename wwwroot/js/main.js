/* ==========================================
   INNOVEST HOLDING - Main JavaScript
   Animations, Interactions, and UI Logic
   ========================================== */

document.addEventListener('DOMContentLoaded', () => {
  
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
  
  function toggleMobileMenu() {
    hamburger.classList.toggle('active');
    mobileNav.classList.toggle('open');
    mobileOverlay.classList.toggle('open');
    document.body.style.overflow = mobileNav.classList.contains('open') ? 'hidden' : '';
  }
  
  if (hamburger) {
    hamburger.addEventListener('click', toggleMobileMenu);
  }
  
  if (mobileOverlay) {
    mobileOverlay.addEventListener('click', toggleMobileMenu);
  }
  
  // Mobile dropdown toggle
  mobileDropdownTriggers.forEach(trigger => {
    trigger.addEventListener('click', (e) => {
      e.preventDefault();
      const dropdownItems = trigger.nextElementSibling;
      if (dropdownItems) {
        dropdownItems.classList.toggle('open');
        trigger.classList.toggle('active');
      }
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
  
  // ==========================================
  // 3. SMOOTH SCROLL FOR ANCHOR LINKS
  // ==========================================
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
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
  const revealElements = document.querySelectorAll('.reveal, .reveal-left, .reveal-right');
  
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
      
      el.textContent = prefix + current.toLocaleString('ar-SA') + suffix;
      
      if (progress < 1) {
        requestAnimationFrame(update);
      } else {
        el.textContent = prefix + target.toLocaleString('ar-SA') + suffix;
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
          heroGlow.style.transform = `scale(${1 + scrolled * 0.0005}) translate(${scrolled * 0.05}px, ${scrolled * 0.02}px)`;
        }
      }
    }, { passive: true });
  }
  
  // ==========================================
  // 8. ACTIVE NAV LINK ON SCROLL
  // ==========================================
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.nav-link[href^="#"]');
  
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
      }
    });
  }
  
  window.addEventListener('scroll', setActiveNav, { passive: true });
  
  // ==========================================
  // 9. FORM HANDLING - handled by inline AJAX scripts
  // ==========================================
  
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
