// Определение тач-устройства по User-Agent в WebGL.
// Input.touchSupported в десктопных Chrome/Safari нередко возвращает true
// (поддержка touch API без сенсорного экрана), поэтому ориентируемся на UA —
// тот же подход, что в web/index.html для подбора стилей.
mergeInto(LibraryManager.library, {
  IsMobileUA: function () {
    var ua = navigator.userAgent || '';
    return /iPhone|iPad|iPod|Android|Mobile/i.test(ua) ? 1 : 0;
  }
});
