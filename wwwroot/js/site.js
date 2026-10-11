// Global site behavior: nav scroll state, scroll-reveal, hero image slider.
// Deliberately minimal - one job per feature, no scattered per-element effects.

document.addEventListener('DOMContentLoaded', function () {

    // Auto-dismiss top alert banners after 4 seconds.
    document.querySelectorAll('.alert').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .5s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 500);
        }, 4000);
    });

    // ---- Nav: solid + blurred once the page scrolls past the hero ----
    var nav = document.querySelector('.site-nav');
    if (nav) {
        var onScroll = function () {
            nav.classList.toggle('is-scrolled', window.scrollY > 40);
        };
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    // ---- Scroll reveal for section heads (IntersectionObserver) ----
    var revealEls = document.querySelectorAll('.reveal');
    if (revealEls.length && 'IntersectionObserver' in window) {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15, rootMargin: '0px 0px -60px 0px' });

        revealEls.forEach(function (el) { io.observe(el); });
    } else {
        revealEls.forEach(function (el) { el.classList.add('is-visible'); });
    }

    // ---- Hero image slider: autoplay, prev/next, dots, swipe, pause-on-hover ----
    var slider = document.getElementById('heroSlider');
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (slider) {
        var slides = Array.prototype.slice.call(slider.querySelectorAll('.hero-slider__slide'));
        var dots = Array.prototype.slice.call(slider.querySelectorAll('.hero-slider__dot'));
        var prevBtn = slider.querySelector('.hero-slider__nav--prev');
        var nextBtn = slider.querySelector('.hero-slider__nav--next');
        var current = 0;
        var timer = null;
        var intervalMs = 4500;

        function goTo(index) {
            if (!slides.length) return;
            current = (index + slides.length) % slides.length;
            slides.forEach(function (s, i) { s.classList.toggle('is-active', i === current); });
            dots.forEach(function (d, i) { d.classList.toggle('is-active', i === current); });
        }

        function next() { goTo(current + 1); }
        function prev() { goTo(current - 1); }

        function startAutoplay() {
            if (reduceMotion || slides.length < 2) return;
            stopAutoplay();
            timer = window.setInterval(next, intervalMs);
        }
        function stopAutoplay() {
            if (timer) { window.clearInterval(timer); timer = null; }
        }

        nextBtn?.addEventListener('click', function () { next(); startAutoplay(); });
        prevBtn?.addEventListener('click', function () { prev(); startAutoplay(); });
        dots.forEach(function (dot, i) {
            dot.addEventListener('click', function () { goTo(i); startAutoplay(); });
        });

        slider.addEventListener('mouseenter', stopAutoplay);
        slider.addEventListener('mouseleave', startAutoplay);

        // Touch swipe support
        var touchStartX = null;
        slider.addEventListener('touchstart', function (e) { touchStartX = e.touches[0].clientX; }, { passive: true });
        slider.addEventListener('touchend', function (e) {
            if (touchStartX === null) return;
            var dx = e.changedTouches[0].clientX - touchStartX;
            if (Math.abs(dx) > 40) { dx > 0 ? prev() : next(); startAutoplay(); }
            touchStartX = null;
        }, { passive: true });

        startAutoplay();
        
    }
});
