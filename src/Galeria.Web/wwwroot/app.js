// Navegación "volver atrás" propia de la app (NavMenu.razor): en vez de history.back() crudo
// (que en Blazor Server puede aterrizar en /Account/Login u otra página fuera de la app, ver
// bug encontrado 31/8), se mantiene una pila de rutas visitadas en sessionStorage — sobrevive
// a los <EditForm data-enhance="false"> que fuerzan un reload completo (y con eso reinician el
// circuito de Blazor), a diferencia de una lista en memoria del lado del servidor.
window.navHistorial = {
    registrar: function (ruta) {
        try {
            var pila = JSON.parse(sessionStorage.getItem('navHistorial') || '[]');
            if (pila.length === 0 || pila[pila.length - 1] !== ruta) {
                pila.push(ruta);
                if (pila.length > 50) {
                    pila.shift();
                }
                sessionStorage.setItem('navHistorial', JSON.stringify(pila));
            }
        } catch (e) {
            // sessionStorage no disponible (modo privado, etc.) — no rompe la navegación normal.
        }
    },

    volver: function () {
        var destino = '';
        try {
            var pila = JSON.parse(sessionStorage.getItem('navHistorial') || '[]');
            if (pila.length > 1) {
                pila.pop();
                destino = pila[pila.length - 1];
                sessionStorage.setItem('navHistorial', JSON.stringify(pila));
            } else {
                sessionStorage.setItem('navHistorial', '[]');
            }
        } catch (e) {
            destino = '';
        }
        Blazor.navigateTo('/' + destino);
    },

    // Usado por formularios de alta abiertos en una pestaña nueva (ej. "Crealo acá" desde
    // Nueva obra): si esta pestaña fue abierta por otra, se cierra sola al terminar en vez de
    // dejar dos pestañas abiertas a la vez.
    cerrarSiEsPopup: function () {
        if (window.opener && window.opener !== window) {
            window.close();
            return true;
        }
        return false;
    }
};
