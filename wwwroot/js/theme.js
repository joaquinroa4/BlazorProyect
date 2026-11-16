/* ==========================================================
   StudyFlow · tema claro / oscuro
   ==========================================================
   El tema se guarda en localStorage (para el script que corre
   antes del primer pintado) y en una cookie (para que el servidor
   renderice el <html> ya con el tema). Se aplica con el atributo
   data-bs-theme de Bootstrap 5.3.
   ========================================================== */

(function () {
    "use strict";

    var STORAGE_KEY = "studyflow-theme";
    var COOKIE_MAX_AGE = 60 * 60 * 24 * 365; /* un año */
    var root = document.documentElement;

    function current() {
        return root.getAttribute("data-bs-theme") === "dark"
            ? "dark"
            : "light";
    }

    /*
       El tema se guarda en dos lugares:
       - localStorage: lo lee el script del <head> antes del primer pintado.
       - cookie: la lee el servidor (App.razor) para renderizar el <html>
         ya con data-bs-theme. Sin la cookie, la navegación mejorada de
         Blazor reemplaza los atributos de <html> con los del documento
         nuevo y el tema se pierde al cambiar de página.
    */
    function persist(theme) {
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (e) {
            /* localStorage puede estar bloqueado: seguimos igual. */
        }

        document.cookie = STORAGE_KEY + "=" + theme +
            "; path=/; max-age=" + COOKIE_MAX_AGE + "; samesite=lax";
    }

    function apply(theme) {
        root.setAttribute("data-bs-theme", theme);
        root.style.colorScheme = theme;
        persist(theme);
    }

    function toggle() {
        var next = current() === "dark" ? "light" : "dark";

        apply(next);

        document.dispatchEvent(
            new CustomEvent("sf-theme-changed", {
                detail: { theme: next }
            })
        );

        return next;
    }

    window.sfTheme = {
        toggle: toggle,
        apply: apply,
        current: current,
        isDark: function () {
            return current() === "dark";
        }
    };

    /* ======================================================
       Barra lateral en pantallas chicas
       ====================================================== */

    window.sfLayout = {
        toggleSidebar: function () {
            document.body.classList.toggle("sf-sidebar-open");
        },
        closeSidebar: function () {
            document.body.classList.remove("sf-sidebar-open");
        }
    };

    document.addEventListener("click", function (event) {
        var link = event.target.closest(
            ".sf-sidebar a.nav-link, .sf-sidebar a.sf-brand"
        );

        if (link) {
            window.sfLayout.closeSidebar();
        }
    });

    /* Cerrar con Escape */
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            window.sfLayout.closeSidebar();
        }
    });
})();
