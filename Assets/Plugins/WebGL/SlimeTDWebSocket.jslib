var SlimeTDWebSocket = {
  $slimeWs: { sockets: {}, nextId: 1 },

  SlimeTD_WsConnect: function (urlPtr, onOpen, onMessage, onClose) {
    var url = UTF8ToString(urlPtr);
    var id = slimeWs.nextId++;
    var ws = new WebSocket(url);
    ws.binaryType = "arraybuffer";
    slimeWs.sockets[id] = ws;

    ws.onopen = function () {
      {{{ makeDynCall('vi', 'onOpen') }}}(id);
    };
    ws.onmessage = function (e) {
      if (!(e.data instanceof ArrayBuffer)) return;
      var bytes = new Uint8Array(e.data);
      var ptr = _malloc(bytes.length);
      HEAPU8.set(bytes, ptr);
      {{{ makeDynCall('viii', 'onMessage') }}}(id, ptr, bytes.length);
      _free(ptr);
    };
    ws.onclose = function (e) {
      delete slimeWs.sockets[id];
      {{{ makeDynCall('vii', 'onClose') }}}(id, e.code);
    };
    return id;
  },

  SlimeTD_WsSend: function (id, ptr, length) {
    var ws = slimeWs.sockets[id];
    if (!ws || ws.readyState !== 1) return 0;
    ws.send(HEAPU8.slice(ptr, ptr + length));
    return 1;
  },

  SlimeTD_WsClose: function (id) {
    var ws = slimeWs.sockets[id];
    if (ws) ws.close();
  }
};

autoAddDeps(SlimeTDWebSocket, '$slimeWs');
mergeInto(LibraryManager.library, SlimeTDWebSocket);
