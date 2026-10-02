mergeInto(LibraryManager.library, {
  VirtualHouse_HasTouch: function () {
    return (navigator.maxTouchPoints > 0 || "ontouchstart" in window) ? 1 : 0;
  }
});
