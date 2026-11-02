mergeInto(LibraryManager.library, {
  SlimeTD_SyncPersistentData: function () {
    if (typeof FS === "undefined" || !FS.syncfs) return;
    FS.syncfs(false, function (err) {
      if (err) console.log("[SlimeTD] IndexedDB 저장 실패: " + err);
    });
  }
});
