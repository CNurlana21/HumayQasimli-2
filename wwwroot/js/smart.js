// Smart UI behaviour: lightbox, live search, relative time, comment actions,
// glow parallax, cursor spotlight, button ripple, scroll progress.
// site.js-ə toxunulmayıb; bu fayl onun ardınca yüklənir.
(function () {
    'use strict';

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /* ------------------------------------------------------------------
       1) Scroll progress bar
       ------------------------------------------------------------------ */
    function initProgress() {
        var bar = document.createElement('div');
        bar.className = 'sm-progress';
        bar.setAttribute('aria-hidden', 'true');
        document.body.appendChild(bar);
        var ticking = false;
        function update() {
            var h = document.documentElement.scrollHeight - window.innerHeight;
            var p = h > 0 ? window.scrollY / h : 0;
            bar.style.transform = 'scaleX(' + Math.min(1, Math.max(0, p)) + ')';
            ticking = false;
        }
        window.addEventListener('scroll', function () {
            if (!ticking) { ticking = true; requestAnimationFrame(update); }
        }, { passive: true });
        update();
    }

    /* ------------------------------------------------------------------
       2) Glow orbs parallax (CSS dəyişənləri --px / --py)
       ------------------------------------------------------------------ */
    function initParallax() {
        if (reduceMotion || !window.matchMedia('(pointer: fine)').matches) return;
        var sections = document.querySelectorAll('.smart-section');
        sections.forEach(function (sec) {
            var raf = null;
            sec.addEventListener('mousemove', function (e) {
                if (raf) return;
                raf = requestAnimationFrame(function () {
                    var r = sec.getBoundingClientRect();
                    sec.style.setProperty('--px', ((e.clientX - r.left) / r.width - 0.5).toFixed(3));
                    sec.style.setProperty('--py', ((e.clientY - r.top) / r.height - 0.5).toFixed(3));
                    raf = null;
                });
            });
            sec.addEventListener('mouseleave', function () {
                sec.style.setProperty('--px', 0);
                sec.style.setProperty('--py', 0);
            });
        });
    }

    /* ------------------------------------------------------------------
       3) Cursor spotlight on cards / chips
       ------------------------------------------------------------------ */
    function initSpotlight() {
        document.addEventListener('mousemove', function (e) {
            var el = e.target.closest ? e.target.closest('.smart-card, .contact-chip') : null;
            if (!el) return;
            var r = el.getBoundingClientRect();
            el.style.setProperty('--mx', (e.clientX - r.left) + 'px');
            el.style.setProperty('--my', (e.clientY - r.top) + 'px');
        }, { passive: true });
    }

    /* ------------------------------------------------------------------
       4) Button ripple
       ------------------------------------------------------------------ */
    function initRipple() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest ? e.target.closest('.smart-section .btn') : null;
            if (!btn || reduceMotion) return;
            var r = btn.getBoundingClientRect();
            var size = Math.max(r.width, r.height) / 2;
            var s = document.createElement('span');
            s.className = 'sm-ripple';
            s.style.width = s.style.height = size + 'px';
            s.style.left = (e.clientX - r.left - size / 2) + 'px';
            s.style.top = (e.clientY - r.top - size / 2) + 'px';
            btn.appendChild(s);
            setTimeout(function () { s.remove(); }, 650);
        });
    }

    /* ------------------------------------------------------------------
       5) Live search (yalnız cari səhifədəki kartlar üzrə)
       ------------------------------------------------------------------ */
    function initSearch() {
        var input = document.getElementById('cardSearch');
        if (!input) return;
        var cards = Array.prototype.slice.call(document.querySelectorAll('[data-card]'));
        var empty = document.getElementById('cardEmpty');
        var counter = document.getElementById('cardCount');

        function norm(s) { return (s || '').toLocaleLowerCase('az'); }

        function apply() {
            var q = norm(input.value.trim());
            var shown = 0;
            cards.forEach(function (c) {
                var hit = !q || norm(c.getAttribute('data-search')).indexOf(q) !== -1;
                c.classList.toggle('is-hidden', !hit);
                if (hit) shown++;
            });
            if (empty) empty.classList.toggle('is-visible', cards.length > 0 && shown === 0);
            if (counter) counter.textContent = q ? (shown + ' / ' + cards.length + ' nəticə') : (cards.length + ' nəticə');
        }
        input.addEventListener('input', apply);
        apply();
    }

    /* ------------------------------------------------------------------
       6) Relative time ("3 saat əvvəl")
       ------------------------------------------------------------------ */
    function relTime(date) {
        var diff = Math.floor((Date.now() - date.getTime()) / 1000);
        if (isNaN(diff)) return null;
        if (diff < 0) diff = 0;
        if (diff < 45) return 'İndicə';
        if (diff < 3600) return Math.max(1, Math.round(diff / 60)) + ' dəqiqə əvvəl';
        if (diff < 86400) return Math.round(diff / 3600) + ' saat əvvəl';
        if (diff < 86400 * 7) return Math.round(diff / 86400) + ' gün əvvəl';
        if (diff < 86400 * 30) return Math.round(diff / (86400 * 7)) + ' həftə əvvəl';
        return null; // köhnə tarixlər üçün mütləq tarix qalır
    }

    function refreshTimes(root) {
        (root || document).querySelectorAll('time[data-rel]').forEach(function (t) {
            var d = new Date(t.getAttribute('datetime'));
            var text = relTime(d);
            if (text) {
                var label = t.querySelector('.t-label');
                if (label) label.textContent = text;
                t.classList.toggle('is-fresh', (Date.now() - d.getTime()) < 3600 * 1000);
            }
        });
    }
    window.smartRefreshTimes = refreshTimes;

    /* ------------------------------------------------------------------
       7) Comment actions: Like / Reply (like yalnız interfeysdədir)
       ------------------------------------------------------------------ */
    function likeKey(id) { return 'sm-comment-like-' + id; }

    function restoreCommentLikes() {
        document.querySelectorAll('.comment-like[data-comment-id]').forEach(function (btn) {
            var id = btn.getAttribute('data-comment-id');
            var liked = false;
            try { liked = localStorage.getItem(likeKey(id)) === '1'; } catch (e) { }
            if (liked) setLiked(btn, true, false);
        });
    }

    function setLiked(btn, liked, changeCount) {
        var span = btn.querySelector('.like-count');
        var icon = btn.querySelector('i');
        var n = parseInt(span ? span.textContent : '0', 10) || 0;
        if (changeCount) n = Math.max(0, n + (liked ? 1 : -1));
        else if (liked && n === 0) n = 1;
        if (span) span.textContent = n;
        btn.classList.toggle('is-liked', liked);
        btn.setAttribute('aria-pressed', liked ? 'true' : 'false');
        if (icon) { icon.className = liked ? 'bi bi-heart-fill' : 'bi bi-heart'; }
    }

    function initCommentActions() {
        document.addEventListener('click', function (e) {
            var like = e.target.closest ? e.target.closest('.comment-like') : null;
            if (like) {
                var willLike = !like.classList.contains('is-liked');
                setLiked(like, willLike, true);
                var id = like.getAttribute('data-comment-id');
                if (id) { try { localStorage.setItem(likeKey(id), willLike ? '1' : '0'); } catch (err) { } }
                return;
            }
            var reply = e.target.closest ? e.target.closest('.comment-reply') : null;
            if (reply) {
                var input = document.getElementById('commentContent');
                if (input) {
                    var who = reply.getAttribute('data-author') || '';
                    input.value = who ? '@' + who + ' ' : '';
                    input.focus();
                    input.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'center' });
                } else {
                    var prompt = document.querySelector('.discussion__login');
                    if (prompt) {
                        prompt.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'center' });
                        prompt.classList.remove('is-flash');
                        void prompt.offsetWidth;
                        prompt.classList.add('is-flash');
                    }
                }
            }
        });

        var input2 = document.getElementById('commentContent');
        var counter = document.getElementById('commentCounter');
        if (input2 && counter) {
            var max = input2.getAttribute('maxlength') || 2000;
            var upd = function () { counter.textContent = input2.value.length + '/' + max; };
            input2.addEventListener('input', upd);
            upd();
        }
    }

    /* ------------------------------------------------------------------
       8) LIGHTBOX
       ------------------------------------------------------------------ */
    function initLightbox() {
        var box = document.getElementById('smLightbox');
        if (!box) return;

        var stage = box.querySelector('.lightbox__stage');
        var img = box.querySelector('.lightbox__img');
        var titleEl = box.querySelector('.lightbox__title');
        var subEl = box.querySelector('.lightbox__sub');
        var counterEl = box.querySelector('.lightbox__counter');
        var btnPrev = box.querySelector('.lightbox__prev');
        var btnNext = box.querySelector('.lightbox__next');
        var btnClose = box.querySelector('.lightbox__close');

        var items = [];
        var index = 0;
        var opener = null;
        var touchX = null;

        function lockScroll(lock) {
            var root = document.documentElement;
            if (lock) {
                var sw = window.innerWidth - root.clientWidth;
                if (sw > 0) document.body.style.paddingRight = sw + 'px';
                root.classList.add('lb-lock');
            } else {
                root.classList.remove('lb-lock');
                document.body.style.paddingRight = '';
            }
        }

        function setZoom(on) {
            stage.classList.toggle('is-zoomed', on);
            if (!on) { img.style.removeProperty('--zx'); img.style.removeProperty('--zy'); }
        }

        function show(i) {
            if (!items.length) return;
            index = (i + items.length) % items.length;
            var el = items[index];
            setZoom(false);
            stage.classList.add('is-loading');
            var src = el.getAttribute('data-lightbox');
            var tmp = new Image();
            tmp.onload = function () {
                if (items[index] !== el) return;
                img.src = src;
                stage.classList.remove('is-loading');
            };
            tmp.onerror = function () { stage.classList.remove('is-loading'); };
            tmp.src = src;
            img.alt = el.getAttribute('data-title') || '';
            titleEl.textContent = el.getAttribute('data-title') || '';
            subEl.textContent = el.getAttribute('data-sub') || '';
            counterEl.textContent = (index + 1) + ' / ' + items.length;
            box.classList.toggle('is-single', items.length < 2);
        }

        function open(el) {
            var group = el.getAttribute('data-lightbox-group');
            var all = Array.prototype.slice.call(document.querySelectorAll('[data-lightbox]'));
            items = group ? all.filter(function (n) { return n.getAttribute('data-lightbox-group') === group; }) : [el];
            opener = el;
            lockScroll(true);
            box.classList.add('is-open');
            box.setAttribute('aria-hidden', 'false');
            show(items.indexOf(el));
            setTimeout(function () { btnClose.focus(); }, 60);
        }

        function close() {
            box.classList.remove('is-open');
            box.setAttribute('aria-hidden', 'true');
            setZoom(false);
            lockScroll(false);
            if (opener) { try { opener.focus(); } catch (e) { } }
            setTimeout(function () { if (!box.classList.contains('is-open')) img.removeAttribute('src'); }, 400);
        }

        document.addEventListener('click', function (e) {
            var trg = e.target.closest ? e.target.closest('[data-lightbox]') : null;
            if (trg) { e.preventDefault(); open(trg); }
        });

        box.querySelectorAll('[data-lb-close]').forEach(function (n) { n.addEventListener('click', close); });
        btnPrev.addEventListener('click', function () { show(index - 1); });
        btnNext.addEventListener('click', function () { show(index + 1); });

        // Böyütmə / kiçiltmə (şəkilə klik)
        stage.addEventListener('click', function (e) {
            if (stage.classList.contains('is-loading')) return;
            var r = img.getBoundingClientRect();
            if (!stage.classList.contains('is-zoomed')) {
                img.style.setProperty('--zx', ((e.clientX - r.left) / r.width * 100) + '%');
                img.style.setProperty('--zy', ((e.clientY - r.top) / r.height * 100) + '%');
            }
            setZoom(!stage.classList.contains('is-zoomed'));
        });
        stage.addEventListener('mousemove', function (e) {
            if (!stage.classList.contains('is-zoomed')) return;
            var r = stage.getBoundingClientRect();
            img.style.setProperty('--zx', ((e.clientX - r.left) / r.width * 100) + '%');
            img.style.setProperty('--zy', ((e.clientY - r.top) / r.height * 100) + '%');
        });

        // Klaviatura
        document.addEventListener('keydown', function (e) {
            if (!box.classList.contains('is-open')) return;
            if (e.key === 'Escape') { close(); }
            else if (e.key === 'ArrowLeft') { show(index - 1); }
            else if (e.key === 'ArrowRight') { show(index + 1); }
            else if (e.key === 'Tab') {
                var f = box.querySelectorAll('button:not([disabled])');
                var visible = Array.prototype.filter.call(f, function (b) { return b.offsetParent !== null; });
                if (!visible.length) return;
                var first = visible[0], last = visible[visible.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        });

        // Swipe
        box.addEventListener('touchstart', function (e) { touchX = e.touches[0].clientX; }, { passive: true });
        box.addEventListener('touchend', function (e) {
            if (touchX === null || stage.classList.contains('is-zoomed')) { touchX = null; return; }
            var dx = e.changedTouches[0].clientX - touchX;
            if (Math.abs(dx) > 50) { show(dx > 0 ? index - 1 : index + 1); }
            touchX = null;
        }, { passive: true });
    }

    /* ------------------------------------------------------------------ */
    document.addEventListener('DOMContentLoaded', function () {
        initProgress();
        initParallax();
        initSpotlight();
        initRipple();
        initSearch();
        refreshTimes();
        restoreCommentLikes();
        initCommentActions();
        initLightbox();
        setInterval(function () { refreshTimes(); }, 60000);
    });
})();


// Hero başlığı: hər hərf ayrıca animasiya olunur
(function () {
    var h = document.querySelector('.hero__title');
    if (!h || h.getAttribute('data-split')) return;
    h.setAttribute('data-split', '1');
    h.setAttribute('aria-label', h.textContent.replace(/\s+/g, ' ').trim());

    var n = 0;
    h.querySelectorAll('.line > span').forEach(function (word) {
        var text = word.textContent;
        word.textContent = '';
        word.setAttribute('aria-hidden', 'true');
        Array.from(text).forEach(function (chr) {
            if (chr === ' ') { word.appendChild(document.createTextNode(' ')); return; }
            var s = document.createElement('span');
            s.className = 'ch';
            s.textContent = chr;
            s.style.setProperty('--i', n++);
            word.appendChild(s);
        });
    });
})();

// "Ətraflı oxu": uzun mətni qısaldır, düymə ilə tam açır
(function () {
    function setup() {
        var blocks = document.querySelectorAll('[data-readmore]');
        if (!blocks.length) return;

        function measure(b) {
            var t = b.querySelector('.readmore__text');
            if (!t || b.classList.contains('is-open')) return;
            b.classList.toggle('is-collapsible', t.scrollHeight > t.clientHeight + 2);
        }

        function measureAll() { blocks.forEach(measure); }

        blocks.forEach(function (b) {
            var t = b.querySelector('.readmore__text');
            var btn = b.querySelector('.readmore__btn');
            if (!t || !btn) return;
            b.classList.add('rm-ready');

            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                var open = !b.classList.contains('is-open');
                var label = btn.querySelector('span');

                if (open) {
                    t.style.maxHeight = t.scrollHeight + 'px';
                    b.classList.add('is-open');
                    var done = function (ev) {
                        if (ev.propertyName !== 'max-height') return;
                        t.removeEventListener('transitionend', done);
                        if (b.classList.contains('is-open')) t.style.maxHeight = 'none';
                    };
                    t.addEventListener('transitionend', done);
                } else {
                    t.style.maxHeight = t.scrollHeight + 'px';
                    void t.offsetHeight;
                    t.style.maxHeight = '';
                    b.classList.remove('is-open');
                }
                btn.setAttribute('aria-expanded', open ? 'true' : 'false');
                if (label) label.textContent = open ? 'Daha az göstər' : 'Ətraflı oxu';
            });
        });

        measureAll();
        window.addEventListener('load', measureAll);
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(measureAll);
        var timer;
        window.addEventListener('resize', function () { clearTimeout(timer); timer = setTimeout(measureAll, 150); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', setup);
    else setup();
})();