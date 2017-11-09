(function () {
    'use strict';

    Storage.prototype.setObject = function (key, value) {
        this.setItem(key, JSON.stringify(value));
    }

    Storage.prototype.getObject = function (key) {
        var value = this.getItem(key);
        return value && JSON.parse(value);
    }

    //from https://github.com/stephenkubovic/angular-visibility-change
    var module = angular.module('visibilityChange', []);
    module.service('VisibilityChange', ['$document', '$rootScope', '$timeout', function ($document, $rootScope, $timeout) {

        var broadcastVisibleEvent = 'pageBecameVisible',
            broadcastHiddenEvent = 'pageBecameHidden',
            broadcastEnabled = false,
            loggingEnabled = false,
            hidden = 'hidden',
            changeCallbacks = [],
            visibleCallbacks = [],
            hiddenCallbacks = [],
            $doc = $document[0],
            visibilityChange;

        this.configure = function (config) {
            if (typeof config !== 'object') {
                throw new Error(config + ' is not a valid configuration object');
            }

            if (config.broadcast === true) {
                broadcastEnabled = true;
            } else if (typeof config.broadcast === 'object') {
                broadcastEnabled = true;
                broadcastVisibleEvent = config.broadcast.visible || broadcastVisibleEvent;
                broadcastHiddenEvent = config.broadcast.hidden || broadcastHiddenEvent;
            }
        };

        this.onChange = function (callback) {
            changeCallbacks.push(callback);
        };

        this.onVisible = function (callback) {
            visibleCallbacks.push(callback);
        };

        this.onHidden = function (callback) {
            hiddenCallbacks.push(callback);
        };

        if (hidden in $doc) {
            visibilityChange = 'visibilitychange';
        } else if ((hidden = 'webkitHidden') in $doc) {
            visibilityChange = 'webkitvisibilitychange';
        } else if ((hidden = 'mozHidden') in $doc) {
            visibilityChange = 'mozvisibilitychange';
        } else if ((hidden = 'msHidden') in $doc) {
            visibilityChange = 'msvisibilitychange';
        } else {
            return;
        }

        var onVisibilityChange = function () {
            $timeout(function () {
                if ($document[0][hidden]) {
                    notifyHidden();
                } else {
                    notifyVisible();
                }
            }, 100);
        };

        var execCallbacks = function () {
            var args = Array.prototype.slice.call(arguments),
                callbacks = args.shift();

            for (var i = 0; i < callbacks.length; i++) {
                callbacks[i].apply(null, args);
            }
        };

        var notifyHidden = function () {
            if (broadcastEnabled) {
                $rootScope.$broadcast(broadcastHiddenEvent);
            }
            execCallbacks(changeCallbacks, false);
            execCallbacks(hiddenCallbacks);
        };

        var notifyVisible = function () {
            if (broadcastEnabled) {
                $rootScope.$broadcast(broadcastVisibleEvent);
            }
            execCallbacks(changeCallbacks, true);
            execCallbacks(visibleCallbacks);
        };

        $document.on(visibilityChange, onVisibilityChange);
    }]);

    var app = angular.module('chunibyo', ['angularSoundManager', 'ui-notification', 'angularLazyImg', 'authentication.service', 'ngScrollbars', 'visibilityChange']);

    app.config(["$httpProvider", function ($httpProvider) {
        $httpProvider.defaults.headers.post['Content-Type'] = 'application/x-www-form-urlencoded';
        $httpProvider.interceptors.push(['$q', '$rootScope', "$injector", function ($q, $rootScope, $injector) {
            return {
                'request': function (config) {
                    $rootScope.loading = true;
                    return $q.resolve(config);
                },
                'response': function (response) {
                    $rootScope.loading = false;
                    return $q.resolve(response);
                },
                'requestError': function (rejection) {
                    $rootScope.loading = false;
                    return $q.reject(rejection);
                },
                'responseError': function (rejection) {
                    $rootScope.loading = false;
                    if (rejection.status === 401) {
                        $rootScope.user.isLogin = false;
                        $injector.get('Notification').warning("同步歌单请先登录");
                    }
                    return $q.reject(rejection);
                }
            }
        }]);
    }]);

    app.config(function (NotificationProvider) {
        NotificationProvider.setOptions({
            delay: 2000,
            startTop: 20,
            startRight: 10,
            verticalSpacing: 20,
            horizontalSpacing: 20,
            positionX: 'center',
            positionY: 'top'
        });
    });

    app.config(function (lazyImgConfigProvider) {
        var scrollable = document.querySelector('#hot-play-list');
        lazyImgConfigProvider.setOptions({
            offset: 0,
            container: angular.element(scrollable)
        });
    });

    app.config(function (ScrollBarsProvider) {
        ScrollBarsProvider.defaults = {
            scrollButtons: {
                scrollAmount: 'auto', // scroll amount when button pressed
                enable: true // enable scrolling buttons by default
            }
        };
    });

    app.filter('playmode_title', function () {
        return function (input) {
            switch (input) {
                case 0:
                    return "顺序";
                    break;
                case 1:
                    return "随机";
                    break;
                case 2:
                    return "单曲循环";
                    break;
            }
        };
    });
    // control main view of page, it can be called any place
    app.controller('NavigationController', ['$scope', '$http',
        '$httpParamSerializerJQLike', '$timeout',
        'angularPlayer', 'Notification', '$rootScope', 'AuthenticationService', 'VisibilityChange',
        function ($scope, $http, $httpParamSerializerJQLike,
            $timeout, angularPlayer, Notification, $rootScope, AuthenticationService, VisibilityChange) {
            $scope.window_url_stack = [];
            $scope.current_tag = 1;
            $scope.is_window_hidden = 1;
            $scope.is_dialog_hidden = 1;

            $scope.songs = [];
            $scope.current_list_id = -1;

            $scope.dialog_song = '';
            $scope.dialog_type = 0;
            $scope.dialog_title = '';

            $rootScope.page_title = "Chunibyo";
            //scrollbar config
            $scope.config = {
                autoHideScrollbar: false,
                theme: 'light',
                advanced: {
                    updateOnContentResize: true
                },
                scrollInertia: 0
            };

            // tag
            $scope.showTag = function (tag_id) {
                $scope.current_tag = tag_id;
                $scope.is_window_hidden = 1;
                $scope.window_url_stack = [];
                $scope.closeWindow();
            };

            // playlist window
            $scope.resetWindow = function () {
                $scope.cover_img_url = '/images/loading.gif';
                $scope.playlist_title = '';
                $scope.songs = [];
            };

            $scope.showWindow = function (url) {
                $scope.is_window_hidden = 0;
                $scope.resetWindow();

                $scope.window_url_stack.push(url);
                $http.get(url).then(function (datas) {
                    var data = datas.data;
                    if (data.status == '0') {
                        Notification.info(data.reason);
                        $scope.popWindow();
                        return;
                    }
                    $scope.songs = data.tracks;
                    $scope.cover_img_url = data.info.coverImgUrl;
                    $scope.playlist_title = data.info.title;
                    $scope.list_id = data.info.id;
                    $scope.is_mine = data.is_mine;
                });
            };

            $scope.closeWindow = function () {
                $scope.is_window_hidden = 1;
                $scope.resetWindow();
                $scope.window_url_stack = [];
            };

            $scope.popWindow = function () {
                $scope.window_url_stack.pop();
                if ($scope.window_url_stack.length === 0) {
                    $scope.closeWindow();
                }
                else {
                    $scope.resetWindow();
                    var url = $scope.window_url_stack[$scope.window_url_stack.length - 1];
                    $http.get(url).then(function (datas) {
                        var data = datas.data;
                        $scope.songs = data.tracks;
                        $scope.cover_img_url = data.info.cover_img_url;
                        $scope.playlist_title = data.info.title;
                    });
                }
            };

            $scope.showPlaylist = function (list_id, my) {
                if (!list_id)
                    return;
                var url = '';
                if (my)
                    url = 'api/myplaylist/list?listId=' + list_id;
                else
                    url = 'api/playlist/list?listId=' + list_id;
                $scope.showWindow(url);
            };

            $scope.showArtist = function (artist_id) {
                if (!artist_id)
                    return;
                var url = 'api/playlist/artist?artistId=' + artist_id;
                $scope.showWindow(url);
            };

            $scope.showAlbum = function (album_id) {
                if (!album_id)
                    return;
                var url = 'api/playlist/album?albumId=' + album_id;
                $scope.showWindow(url);
            };

            $scope.directplaylist = function (list_id) {
                if (!list_id)
                    return;
                var url = '/api/myplaylist/list?listId=' + list_id;

                $http.get(url).then(function (datas) {
                    var data = datas.data;
                    $scope.songs = data.tracks;
                    $scope.current_list_id = list_id;

                    $timeout(function () {
                        // use timeout to avoid stil in digest error.
                        angularPlayer.clearPlaylist(function (data) {
                            //add songs to playlist
                            angularPlayer.addTrackArray($scope.songs);
                            //play first song
                            var index = 0;
                            if (angularPlayer.getShuffle()) {
                                var max = $scope.songs.length - 1;
                                var min = 0;
                                index = Math.floor(Math.random() * (max - min + 1)) + min;
                            }
                            angularPlayer.playTrack($scope.songs[index].id);

                        });
                    }, 0);
                });
            };

            $scope.showDialog = function (dialog_type, data) {
                if (!data) {
                    Notification.warning("当前无歌曲");
                    return;
                }
                $scope.is_dialog_hidden = 0;
                var dialogWidth = 480;
                var left = $(window).width() / 2 - dialogWidth / 2;
                $scope.myStyle = { 'left': left + 'px' };

                if (dialog_type == 0) {
                    $scope.dialog_title = '添加到歌单';
                    var url = '/api/myplaylist/show';
                    $scope.dialog_song = data;
                    $http.get(url).then(function (datas) {
                        var data = datas.data;
                        $scope.myplaylist = data.result;
                    });
                }
            };

            $scope.chooseDialogOption = function (option_id) {
                var url = '/api/myplaylist/add';

                $http({
                    url: url,
                    method: 'POST',
                    data: $httpParamSerializerJQLike({
                        listId: option_id,
                        id: $scope.dialog_song.id,
                        title: $scope.dialog_song.title,
                        artist: $scope.dialog_song.artist,
                        url: $scope.dialog_song.url,
                        artistId: $scope.dialog_song.artistId,
                        album: $scope.dialog_song.album,
                        albumId: $scope.dialog_song.albumId,
                        source: $scope.dialog_song.source,
                        sourceUrl: $scope.dialog_song.sourceUrl,
                        imgUrl: $scope.dialog_song.imgUrl,
                        lyricUrl: $scope.dialog_song.lyricUrl
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                }).then(function (result) {
                    if (result.data.success) {
                        Notification.success('添加到歌单成功');
                        $scope.closeDialog();
                        // add to current playing list
                        if (option_id == $scope.current_list_id) {
                            angularPlayer.addTrack($scope.dialog_song);
                        }
                    } else {
                        Notification.error(result.data.message);
                    }
                });
            };

            $scope.newDialogOption = function () {
                $scope.dialog_type = 1;
            };

            $scope.cancelNewDialog = function () {
                $scope.dialog_type = 0;
            };

            $scope.createAndAddPlaylist = function () {
                var url = '/api/myplaylist/create';
                if (!$scope.dialog_song)
                    return;
                $http({
                    url: url,
                    method: 'POST',
                    data: $httpParamSerializerJQLike({
                        listTitle: $scope.newlist_title,
                        id: $scope.dialog_song.id,
                        title: $scope.dialog_song.title,
                        artist: $scope.dialog_song.artist,
                        url: $scope.dialog_song.url,
                        artistId: $scope.dialog_song.artistId,
                        album: $scope.dialog_song.album,
                        albumId: $scope.dialog_song.albumId,
                        source: $scope.dialog_song.source,
                        sourceUrl: $scope.dialog_song.sourceUrl,
                        imgUrl: $scope.dialog_song.imgUrl,
                        lyricUrl: $scope.dialog_song.lyricUrl
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                }).then(function (result) {
                    if (result.data.success) {
                        $rootScope.$broadcast('myplaylist:update');
                        Notification.success('添加到歌单成功');
                        $scope.closeDialog();
                    }
                    else {
                        Notification.error(result.data.message);
                    }
                });
            };

            $scope.removeSongFromPlaylist = function (song, list_id) {
                var url = '/api/myplaylist/removetrack';

                $http({
                    url: url,
                    method: 'POST',
                    data: $httpParamSerializerJQLike({
                        listId: list_id,
                        trackId: song.id
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                }).then(function (result) {
                    if (result.data.success) {
                        // remove song from songs
                        var index = $scope.songs.indexOf(song);
                        if (index > -1) {
                            $scope.songs.splice(index, 1);
                        }
                        Notification.success('删除成功');
                    } else {
                        Notification.error(result.data.message);
                    }

                });
            }

            $scope.closeDialog = function () {
                $scope.is_dialog_hidden = 1;
                $scope.dialog_type = 0;
            };

            $scope.setCurrentList = function (list_id) {
                $scope.current_list_id = list_id;
            };

            $scope.playmylist = function (list_id) {
                $timeout(function () {
                    angularPlayer.clearPlaylist(function (data) {
                        //add songs to playlist
                        angularPlayer.addTrackArray($scope.songs);
                        var index = 0;
                        if (angularPlayer.getShuffle()) {
                            var max = $scope.songs.length - 1;
                            var min = 0;
                            index = Math.floor(Math.random() * (max - min + 1)) + min;
                        }
                        //play first song
                        angularPlayer.playTrack($scope.songs[index].id);
                    });
                }, 0);
                $scope.setCurrentList(list_id);
            };

            $scope.removemylist = function (list_id) {
                var url = '/api/myplaylist/remove';

                $http({
                    url: url,
                    method: 'POST',
                    data: $httpParamSerializerJQLike({
                        listId: list_id,
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                }).then(function (result) {
                    if (result.data.success) {
                        $rootScope.$broadcast('myplaylist:update');
                        $scope.closeWindow();
                        Notification.success('删除成功');
                    } else {
                        Notification.error(result.data.message);
                    }

                });
            };

            $scope.clonelist = function (list_id) {
                var url = '/api/myplaylist/clone';

                $http({
                    url: url,
                    method: 'POST',
                    data: $httpParamSerializerJQLike({
                        listId: list_id,
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                }).then(function (result) {
                    if (result.data.success) {
                        $rootScope.$broadcast('myplaylist:update');
                        $scope.closeWindow();
                        Notification.success('收藏到我的歌单成功');
                    } else {
                        Notification.error(result.data.message);
                    }

                });
            };

            //login & register
            $rootScope.user = { isLogin: true };

            var timer;
            VisibilityChange.onChange(function (visible) {
                if (visible) {
                    $rootScope.page_title = '(*´∇｀*)  被你发现啦~ ' + $rootScope.origin_title;
                    timer = $timeout(function () {
                        $rootScope.page_title = $rootScope.origin_title;
                    }, 4000);
                } else {
                    $rootScope.origin_title = $rootScope.page_title;
                    $rootScope.page_title = "(つェ⊂)我藏好了哦";
                    $timeout.cancel(timer);
                }
            });

        }]);


    app.controller('PlayController', ['$scope', '$timeout', '$log',
        '$anchorScroll', '$location', 'angularPlayer', '$http',
        '$httpParamSerializerJQLike', '$rootScope', 'Notification',
        function ($scope, $timeout, $log, $anchorScroll, $location, angularPlayer,
            $http, $httpParamSerializerJQLike, $rootScope, Notification) {
            $scope.menuHidden = true;
            $scope.volume = angularPlayer.getVolume();
            $scope.mute = angularPlayer.getMuteStatus();
            $scope.settings = { "playmode": 0, "nowplaying_track_id": -1 };
            $scope.lyricArray = [];
            $scope.lyricLineNumber = -1;
            $scope.lastTrackId = null;
            $scope.show_lyric = true;
            function switchMode(mode) {
                //playmodeplaymode 0:loop 1:shuffle 2:repeat one
                switch (mode) {
                    case 0:
                        if (angularPlayer.getShuffle()) {
                            angularPlayer.toggleShuffle();
                        }
                        angularPlayer.setRepeatOneStatus(false);
                        break;
                    case 1:
                        if (!angularPlayer.getShuffle()) {
                            angularPlayer.toggleShuffle();
                        }
                        angularPlayer.setRepeatOneStatus(false);
                        break;
                    case 2:
                        if (angularPlayer.getShuffle()) {
                            angularPlayer.toggleShuffle();
                        }
                        angularPlayer.setRepeatOneStatus(true);
                        break
                }
            }

            $scope.loadLocalSettings = function () {
                var defaultSettings = { "playmode": 0, "nowplaying_track_id": -1 }
                var localSettings = localStorage.getObject('player-settings');
                if (localSettings == null) {
                    $scope.settings = { "playmode": 0, "nowplaying_track_id": -1 };
                    $scope.saveLocalSettings();
                }
                else {
                    $scope.settings = localSettings;
                }
                // apply settings
                switchMode($scope.settings.playmode);
            }

            $scope.saveLocalSettings = function () {
                localStorage.setObject('player-settings', $scope.settings);
            }

            $scope.loadLocalCurrentPlaying = function () {
                var localSettings = localStorage.getObject('current-playing');
                if (localSettings == null) {
                    return;
                }
                // apply local current playing;
                angularPlayer.addTrackArray(localSettings);
            }

            $scope.saveLocalCurrentPlaying = function () {
                localStorage.setObjct('current-playing', angularPlayer.playlist)
            }

            $scope.changePlaymode = function () {
                var playmodeCount = 3;
                $scope.settings.playmode = ($scope.settings.playmode + 1) % playmodeCount;
                switchMode($scope.settings.playmode);
                $scope.saveLocalSettings();
            };

            $scope.$on('music:volume', function (event, data) {
                $scope.$apply(function () {
                    $scope.volume = data;
                });
            });

            $scope.$on('angularPlayer:ready', function (event, data) {
                $log.debug('cleared, ok now add to playlist');
                if (angularPlayer.getRepeatStatus() == false) {
                    angularPlayer.repeatToggle();
                }
                //add songs to playlist
                var localCurrentPlaying = localStorage.getObject('current-playing');
                if (localCurrentPlaying == null) {
                    return;
                }
                angularPlayer.addTrackArray(localCurrentPlaying);

                var localPlayerSettings = localStorage.getObject('player-settings');
                if (localPlayerSettings == null) {
                    return;
                }
                var track_id = localPlayerSettings.nowplaying_track_id;
                if (track_id != -1) {
                    angularPlayer.playTrack(track_id);
                }
                else {
                    angularPlayer.play();
                }
                // disable open and play feature
                angularPlayer.pause();
                angularPlayer.setAnimationFPS(20);
                angularPlayer.tick();
            });

            $scope.gotoAnchor = function (newHash) {
                if ($location.hash() !== newHash) {
                    // set the $location.hash to `newHash` and
                    // $anchorScroll will automatically scroll to it
                    $location.hash(newHash);
                    $anchorScroll();
                } else {
                    // call $anchorScroll() explicitly,
                    // since $location.hash hasn't changed
                    $anchorScroll();
                }
            };

            $scope.togglePlaylist = function () {
                var anchor = "song" + angularPlayer.getCurrentTrack();
                $scope.menuHidden = !$scope.menuHidden;
                if (!$scope.menuHidden) {
                    $scope.gotoAnchor(anchor);
                }
            };

            $scope.toggleMuteStatus = function () {
                // mute function is indeed toggle mute status.
                angularPlayer.mute();
            }

            $scope.myProgress = 0;
            $scope.changingProgress = false;

            $rootScope.$on('track:progress', function (event, data) {
                if ($scope.changingProgress == false) {
                    $scope.myProgress = data;
                }
            });

            $rootScope.$on('track:myprogress', function (event, data) {
                $scope.$apply(function () {
                    // should use apply to force refresh ui
                    $scope.myProgress = data;
                });
            });

            $scope.tempVolume = 0;
            $scope.$on('music:mute', function (event, data) {
                $scope.mute = data;
                if (data) {
                    $scope.tempVolume = $scope.volume;
                    $timeout(function () { angularPlayer.adjustVolumeSlider(0) }, 0);
                } else {
                    $timeout(function () { angularPlayer.adjustVolumeSlider($scope.volume > 0 ? $scope.volume : $scope.tempVolume) }, 0);
                }
            });

            function parseLyric(lyric) {
                var lines = lyric.split('\n');
                var result = [];
                var timeResult = [];
                var timeRegResult = null;

                function rightPadding(str, length, padChar) {
                    var newstr = str;
                    for (var i = 0; i < length - str.length; i++) {
                        newstr += padChar;
                    }
                    return newstr;
                }

                for (var i = 0; i < lines.length; i++) {
                    var line = lines[i];
                    var tagReg = /\[\D*:([^\]]+)\]/g;
                    var tagRegResult = tagReg.exec(line);
                    if (tagRegResult) {
                        var lyricObject = {};
                        lyricObject.seconds = 0;
                        lyricObject.content = tagRegResult[1];
                        result.push(lyricObject);
                        continue;
                    }

                    var timeReg = /\[(\d{2,})\:(\d{2})(?:\.(\d{1,3}))?\]/g;

                    while (timeRegResult = timeReg.exec(line)) {
                        var content = line.replace(/\[(\d{2,})\:(\d{2})(?:\.(\d{1,3}))?\]/g, '');
                        var min = parseInt(timeRegResult[1]);
                        var sec = parseInt(timeRegResult[2]);
                        var microsec = 0;
                        if (timeRegResult[3] != null) {
                            microsec = parseInt(rightPadding(timeRegResult[3], 3, '0'));
                        }
                        var seconds = min * 60 * 1000 + sec * 1000 + microsec;
                        var lyricObject = {};
                        lyricObject.content = content;
                        lyricObject.seconds = seconds;
                        timeResult.push(lyricObject);
                    }
                }

                // sort time line
                timeResult.sort(function (a, b) {
                    var keyA = a.seconds,
                        keyB = b.seconds;
                    // Compare the 2 dates
                    if (keyA < keyB) return -1;
                    if (keyA > keyB) return 1;
                    return 0;
                });

                // disable tag info, because music provider always write
                // tag info in lyric timeline.
                //result.push.apply(result, timeResult);
                result = timeResult;

                for (var i = 0; i < result.length; i++) {
                    result[i].lineNumber = i;
                }
                if (result.length === 0 && lines.length > 0) {
                    for (var i = 0; i < lines.length; i++) {
                        var line = lines[i];
                        lyricObject = {};
                        lyricObject.seconds = -1;
                        lyricObject.content = line;
                        lyricObject.lineNumber = i;
                        result.push(lyricObject);
                    }
                    lyricObject = {};
                    lyricObject.seconds = -1;
                    lyricObject.content = "(歌词暂不支持滚动)";
                    lyricObject.lineNumber = i;
                    result.push(lyricObject);
                }
                return result;
            }

            $scope.$on('track:id', function (event, data) {
                if ($scope.lastTrackId == data) {
                    return;
                }
                if (data == null) {
                    $scope.lastTrackId = null;
                    var current = localStorage.getObject('player-settings');
                    current.nowplaying_track_id = -1;
                    localStorage.setObject('player-settings', current);
                    $rootScope.page_title = '■ ';
                    return;
                }
                var current = localStorage.getObject('player-settings');
                current.nowplaying_track_id = data;
                localStorage.setObject('player-settings', current);
                //update lyric
                $scope.lyricArray = [];
                $scope.lyricLineNumber = -1;
                $timeout(function () {
                    $scope.updateScrollbar('scrollTo', 0, { scrollInertia: 500 });
                });
                var url = '/api/playlist/lyric?trackId=' + data;
                var track = angularPlayer.getTrack(data);
                $rootScope.page_title = '▶ ' + track.title ? track.title : '' + ' - ' + track.artist ? track.artist : '';
                if (track.lyricUrl != null) {
                    url = url + '&lyricUrl=' + encodeURIComponent(track.lyricUrl);
                }
                if ($scope.lastTrackId)
                    $('.menu').removeClass($scope.lastTrackId);
                $('.menu').removeClass('default');
                if (track.imgUrl) {
                    if ($('#style_' + data).length == 0) {
                        var styleTag = $('<style id="style_' + data + '">.m-playbar .menu.' + data + '::before{background-image:url(' + track.imgUrl + ');}</style>');
                        $('html > head').append(styleTag);
                    }
                    $('.menu').addClass(data);
                } else {
                    $('.menu').addClass('default');
                }
                $http.get(url).then(function (datas) {
                    var data = datas.data;
                    var lyric = data.lyric;
                    if (lyric == null) {
                        return;
                    }
                    $scope.lyricArray = parseLyric(lyric);
                });
                $scope.lastTrackId = data;
            });

            $scope.$on('currentTrack:position', function (event, data) {
                // update lyric position
                if (!$scope.show_lyric)
                    return;
                var currentSeconds = data;
                var lastObject = null;
                for (var i = 0; i < $scope.lyricArray.length; i++) {
                    var lyricObject = $scope.lyricArray[i];
                    if (lyricObject.seconds === -1 || currentSeconds < lyricObject.seconds) {
                        break;
                    }
                    lastObject = lyricObject;
                }
                if (lastObject && lastObject.lineNumber != $scope.lyricLineNumber) {
                    var lineHeight = 20;
                    var lineElement = $(".lyric p")[lastObject.lineNumber];
                    var windowHeight = 270;
                    var offset = lineElement.offsetTop - windowHeight / 2;
                    //$(".lyric").animate({ scrollTop: offset + "px" }, 500);
                    $timeout(function () {
                        $scope.updateScrollbar('scrollTo', offset, { scrollInertia: 500 });
                    });
                    $scope.lyricLineNumber = lastObject.lineNumber;
                }
            });
            $scope.last = null;
            $scope.fps = 0;
            $scope.offset = 0;
            angularPlayer.setShowFrequency(true);
            var canvas = document.querySelector('#darwCanvas');
            var canvasCtx = canvas.getContext('2d');
            var _canvasBuffer = document.createElement('canvas');
            _canvasBuffer.width = canvas.width;
            _canvasBuffer.height = canvas.height;
            var _canvasBufferContext = _canvasBuffer.getContext('2d');
            var lastData = [];
            $scope.$on('currentTrack:Frequency', function (event, data) {
                //if (!$scope.last)
                //    $scope.last = Date.now();
                //$scope.offset = Date.now() - $scope.last;
                //$scope.fps += 1;
                //if ($scope.offset >= 1000) {
                //    console.log($scope.last + '===' + $scope.fps);
                //    $scope.last += $scope.offset;
                //    $scope.fps = 0;
                //}
                if ($scope.show_lyric)
                    return;
                var width = _canvasBuffer.width;
                var height = _canvasBuffer.height;
                var center = height / 2;
                canvasCtx.clearRect(0, 0, width, height);
                _canvasBufferContext.clearRect(0, 0, width, height);

                var barWidth = (data === null || data.length === 0) ? 0 : (width / data.length) * 2.5;
                var barHeight;
                var x = 0;
                var color = _canvasBufferContext.createLinearGradient(0, center, 0, height);
                color.addColorStop(0, '#222222');
                color.addColorStop(1, 'rgba(0,0,0,0)');
                for (var i = 0; i < data.length; i++) {
                    var z = data[i];
                    if (lastData.length >= i) {
                        if (z < lastData[i]) {
                            z = Math.max(lastData[i] - 1, 0);
                        }
                    }
                    barHeight = data[i] / 255.0 * center;
                    _canvasBufferContext.fillStyle = 'rgb(34,34,34)';
                    _canvasBufferContext.fillRect(x, center - z / 255.0 * center - 2, barWidth, 2);
                    _canvasBufferContext.fillStyle = 'rgb(' + Math.min(255, Math.round(data[i] / 2 + 100)) + ',50,50)';
                    _canvasBufferContext.fillRect(x, center - barHeight, barWidth, barHeight);
                    _canvasBufferContext.fillStyle = color;
                    _canvasBufferContext.fillRect(x, center, barWidth, barHeight);
                    x += barWidth + 1;
                }
                canvasCtx.drawImage(_canvasBuffer, 0, 0);
                lastData = data;
            });
            $scope.$on('player:playlist', function (event, data) {
                localStorage.setObject('current-playing', data);
                if (data.length == 0) {
                    $('.menu').attr('class', 'menu default');
                    if ($scope.show_lyric) {
                        $scope.lyricArray = [];
                        $scope.lyricLineNumber = -1;
                    } else {
                        canvasCtx.clearRect(0, 0, canvas.width, canvas.height);
                    }
                }
            });
        }]);

    app.controller('InstantSearchController', ['$scope', '$http', '$timeout', 'angularPlayer',
        function ($scope, $http, $timeout, angularPlayer) {
            $scope.tab = 0;
            $scope.keywords = '';
            $scope.loading = false;

            $scope.changeTab = function (newTab) {
                $scope.loading = true;
                $scope.tab = newTab;
                $scope.result = [];
                $http.get('api/playlist/search?source=' + $scope.tab + '&keywords=' + $scope.keywords).then(function (datas) {
                    var data = datas.data;
                    // update the textarea
                    $scope.result = data.result;
                    $scope.loading = false;
                });
            };

            $scope.isActiveTab = function (tab) {
                return $scope.tab === tab;
            };

            $scope.$watch('keywords', function (tmpStr) {
                if (!tmpStr || tmpStr.length === 0) {
                    $scope.result = [];
                    return 0;
                }
                // if searchStr is still the same..
                // go ahead and retrieve the data
                if (tmpStr === $scope.keywords) {
                    $scope.loading = true;
                    $http.get('api/playlist/search?source=' + $scope.tab + '&keywords=' + $scope.keywords).then(function (datas) {
                        var data = datas.data;
                        // update the textarea
                        $scope.result = data.result;
                        $scope.loading = false;
                    });
                }
            });
        }]);

    app.directive('errSrc', function () {
        // http://stackoverflow.com/questions/16310298/if-a-ngsrc-path-resolves-to-a-404-is-there-a-way-to-fallback-to-a-default
        return {
            link: function (scope, element, attrs) {
                element.bind('error', function () {
                    if (attrs.src != attrs.errSrc) {
                        attrs.$set('src', attrs.errSrc);
                    }
                });
                attrs.$observe('ngSrc', function (value) {
                    if (!value && attrs.errSrc) {
                        attrs.$set('src', attrs.errSrc);
                    }
                });
            }
        }
    });

    app.directive('resize', function ($window) {
        return function (scope, element) {
            var w = angular.element($window);
            var changeHeight = function () {
                var headerHeight = 90;
                var footerHeight = 90;
                element.css('height', (w.height() - headerHeight - footerHeight) + 'px');
            };
            w.bind('resize', function () {
                changeHeight();   // when window size gets changed             
            });
            changeHeight(); // when page loads          
        };
    });

    app.directive('addAndPlay', ['angularPlayer', function (angularPlayer) {
        return {
            restrict: "EA",
            scope: {
                song: "=addAndPlay"
            },
            link: function (scope, element, attrs) {
                element.bind('click', function (event) {
                    angularPlayer.addTrack(scope.song);
                    angularPlayer.playTrack(scope.song.id);
                });
            }
        };
    }]);

    app.directive('addWithoutPlay', ['angularPlayer', 'Notification',
        function (angularPlayer, Notification) {
            return {
                restrict: "EA",
                scope: {
                    song: "=addWithoutPlay"
                },
                link: function (scope, element, attrs) {
                    element.bind('click', function (event) {
                        angularPlayer.addTrack(scope.song);
                        Notification.success("已添加到当前播放歌单");
                    });
                }
            };
        }]);

    app.directive('openSongSource', ['angularPlayer', '$window',
        function (angularPlayer, $window) {
            return {
                restrict: "EA",
                scope: {
                    song: "=openSongSource"
                },
                link: function (scope, element, attrs) {
                    element.bind('click', function (event) {
                        $window.open(scope.song.source_url, '_blank');
                    });
                }
            };
        }]);

    app.directive('infiniteScroll', ['$window',
        function ($window) {
            return {
                restrict: "EA",
                scope: {
                    infiniteScroll: '&',
                    contentSelector: '=contentSelector'
                },
                link: function (scope, elements, attrs) {
                    elements.bind('scroll', function (event) {
                        if (scope.loading) {
                            return;
                        }
                        var containerElement = elements[0];
                        var contentElement = document.querySelector(scope.contentSelector);

                        var baseTop = containerElement.getBoundingClientRect().top;
                        var currentTop = contentElement.getBoundingClientRect().top;
                        var baseHeight = containerElement.offsetHeight;
                        var offset = baseTop - currentTop;

                        var bottom = offset + baseHeight;
                        var height = contentElement.offsetHeight;

                        var remain = height - bottom;
                        var offsetToload = 10;
                        if (remain <= offsetToload) {
                            scope.$apply(scope.infiniteScroll);
                        }
                    });

                }
            };
        }]);

    app.directive('draggable', ['angularPlayer', '$document', '$rootScope',
        function (angularPlayer, $document, $rootScope) {
            return function (scope, element, attrs) {
                var x;
                var container;
                var mode = attrs.mode;
                //element.on('mousedown', function (event) {
                //    scope.changingProgress = true;
                //    container = document.getElementById('progressbar').getBoundingClientRect();
                //    // Prevent default dragging of selected content
                //    event.preventDefault();
                //    x = event.clientX - container.left;
                //    setPosition();
                //    $document.on('mousemove', mousemove);
                //    $document.on('mouseup', mouseup);

                //});

                //function mousemove(event) {
                //    x = event.clientX - container.left;
                //    setPosition();
                //}

                //function changeProgress(progress) {
                //    if (angularPlayer.getCurrentTrack() === null) {
                //        return;
                //    }
                //    var sound = soundManager.getSoundById(angularPlayer.getCurrentTrack());
                //    var duration = sound.durationEstimate;
                //    sound.setPosition(progress * duration);
                //}

                //function setPosition() {
                //    if (container) {
                //        if (x < 0) {
                //            x = 0;
                //        } else if (x > container.right - container.left) {
                //            x = container.right - container.left;
                //        }
                //    }
                //    var progress = x / (container.right - container.left);
                //    $rootScope.$broadcast('track:myprogress', progress * 100);
                //}

                //function mouseup() {
                //    var progress = x / (container.right - container.left);
                //    changeProgress(progress);
                //    $document.off('mousemove', mousemove);
                //    $document.off('mouseup', mouseup);
                //    scope.changingProgress = false;
                //}
                function onMyMousedown() {
                    if (mode == 'play') {
                        scope.changingProgress = true;
                    }
                }

                function onMyMouseup() {
                    if (mode == 'play') {
                        scope.changingProgress = false;
                    }
                }

                function onMyUpdateProgress(progress) {
                    if (mode == 'play') {
                        $rootScope.$broadcast('track:myprogress', progress * 100);
                    }
                    if (mode == 'volume') {
                        angularPlayer.adjustVolumeSlider(progress * 100);
                        if (angularPlayer.getMuteStatus() == true) {
                            angularPlayer.mute();
                        }
                    }
                }

                function onMyCommitProgress(progress) {
                    if (mode == 'play') {
                        if (angularPlayer.getCurrentTrack() === null) {
                            return;
                        }
                        var sound = soundManager.getSoundById(angularPlayer.getCurrentTrack());
                        var duration = sound.durationEstimate;
                        sound.setPosition(progress * duration);
                    }
                    if (mode == 'volume') {
                        var current = localStorage.getObject('player-settings');
                        current.volume = progress * 100;
                        localStorage.setObject('player-settings', current);
                    }
                }

                element.on('mousedown', function (event) {
                    onMyMousedown();
                    container = document.getElementById(attrs.id).getBoundingClientRect();
                    // Prevent default dragging of selected content
                    event.preventDefault();
                    x = event.clientX - container.left;
                    updateProgress();
                    $document.on('mousemove', mousemove);
                    $document.on('mouseup', mouseup);

                });

                function mousemove(event) {
                    x = event.clientX - container.left;
                    updateProgress();
                }

                function mouseup() {
                    var progress = x / (container.right - container.left);
                    commitProgress(progress);
                    $document.off('mousemove', mousemove);
                    $document.off('mouseup', mouseup);
                    onMyMouseup();
                }

                function commitProgress(progress) {
                    onMyCommitProgress(progress);
                }

                function updateProgress() {
                    if (container) {
                        if (x < 0) {
                            x = 0;
                        } else if (x > container.right - container.left) {
                            x = container.right - container.left;
                        }
                    }
                    var progress = x / (container.right - container.left);
                    onMyUpdateProgress(progress);
                }
            };
        }]);

    app.directive('toggleSwitch', ['$compile', function ($compile) {
        return {
            restrict: 'EA',
            replace: true,
            require: 'ngModel',
            scope: {
                isDisabled: '=',
                onLabel: '@',
                offLabel: '@',
                knobLabel: '@',
                html: '=',
                onChange: '&'
            },
            template:
            '<div class="ats-switch" ng-click="toggle()" ng-keypress="onKeyPress($event)" ng-class="{ \'disabled\': isDisabled }" role="switch" aria-checked="{{!!model}}">' +
            '<div class="switch-animate" ng-class="{\'switch-off\': !model, \'switch-on\': model}">' +
            '<span class="switch-left"></span>' +
            '<span class="knob"></span>' +
            '<span class="switch-right"></span>' +
            '</div>' +
            '</div>',
            compile: function (element, attrs) {
                if (angular.isUndefined(attrs.onLabel)) {
                    attrs.onLabel = 'On';
                }
                if (angular.isUndefined(attrs.offLabel)) {
                    attrs.offLabel = 'Off';
                }
                if (angular.isUndefined(attrs.knobLabel)) {
                    attrs.knobLabel = '\u00a0';
                }
                if (angular.isUndefined(attrs.isDisabled)) {
                    attrs.isDisabled = false;
                }
                if (angular.isUndefined(attrs.html)) {
                    attrs.html = false;
                }
                if (angular.isUndefined(attrs.tabindex)) {
                    attrs.tabindex = 0;
                }

                return function postLink(scope, iElement, iAttrs, ngModel) {
                    iElement.attr('tabindex', attrs.tabindex);

                    scope.toggle = function toggle() {
                        if (!scope.isDisabled) {
                            scope.model = !scope.model;
                            ngModel.$setViewValue(scope.model);
                        }
                        scope.onChange();
                    };

                    var spaceCharCode = 32;
                    scope.onKeyPress = function onKeyPress($event) {
                        if ($event.charCode == spaceCharCode && !$event.altKey && !$event.ctrlKey && !$event.metaKey) {
                            scope.toggle();
                        }
                    };

                    ngModel.$formatters.push(function (modelValue) {
                        return modelValue;
                    });

                    ngModel.$parsers.push(function (viewValue) {
                        return viewValue;
                    });

                    ngModel.$viewChangeListeners.push(function () {
                        scope.$eval(attrs.ngChange);
                    });

                    ngModel.$render = function () {
                        scope.model = ngModel.$viewValue;
                    };

                    var bindSpan = function (span, html) {
                        span = angular.element(span);
                        var bindAttributeName = (html === true) ? 'ng-bind-html' : 'ng-bind';

                        // remove old ng-bind attributes
                        span.removeAttr('ng-bind-html');
                        span.removeAttr('ng-bind');

                        if (angular.element(span).hasClass("switch-left"))
                            span.attr(bindAttributeName, 'onLabel');
                        if (span.hasClass("knob"))
                            span.attr(bindAttributeName, 'knobLabel');
                        if (span.hasClass("switch-right"))
                            span.attr(bindAttributeName, 'offLabel');

                        $compile(span)(scope, function (cloned, scope) {
                            span.replaceWith(cloned);
                        });
                    };

                    // add ng-bind attribute to each span element.
                    // NOTE: you need angular-sanitize to use ng-bind-html
                    var bindSwitch = function (iElement, html) {
                        angular.forEach(iElement[0].children[0].children, function (span, index) {
                            bindSpan(span, html);
                        });
                    };

                    scope.$watch('html', function (newValue) {
                        bindSwitch(iElement, newValue);
                    });
                };
            }
        };
    }]);


    app.controller('MyPlayListController', ['$http', '$scope', '$timeout',
        'angularPlayer', function ($http, $scope, $timeout, angularPlayer) {
            $scope.myplaylists = [];

            $scope.loadMyPlaylist = function () {
                $http.get('/api/myplaylist/show').then(function (datas) {
                    var data = datas.data;
                    $scope.myplaylists = data.result;
                });
            };

            $scope.$watch('current_tag', function (newValue, oldValue) {
                if (newValue !== oldValue) {
                    if (newValue == '1') {
                        $scope.myplaylists = [];
                        $scope.loadMyPlaylist();
                    }
                }
            });
            $scope.$on('myplaylist:update', function (event, data) {
                $scope.loadMyPlaylist();
            });

        }]);

    app.controller('PlayListController', ['$http', '$scope', '$timeout',
        'angularPlayer', function ($http, $scope, $timeout, angularPlayer) {
            $scope.result = [];

            $scope.tab = 0;

            $scope.changeTab = function (newTab) {
                $scope.tab = newTab;
                $scope.result = [];
                $http.get('/api/PlayList/Load?source=' + $scope.tab).then(function (datas) {
                    var data = datas.data;
                    $scope.result = data.result;
                });
            };

            $scope.scrolling = function () {
                if ($scope.loading == true) {
                    return
                }
                var offset = $scope.result.length;
                $http.get('/api/PlayList/Load?source=' + $scope.tab + '&offset=' + offset).then(function (datas) {
                    var data = datas.data;
                    $scope.result = $scope.result.concat(data.result);
                });
            }

            $scope.isActiveTab = function (tab) {
                return $scope.tab === tab;
            };


            $scope.loadPlaylist = function () {
                $http.get('/api/PlayList/Load?source=' + $scope.tab).then(function (datas) {
                    var data = datas.data;
                    $scope.result = data.result;
                });
            };
        }]);
    app.controller('LoginController', ['AuthenticationService', 'Notification', '$rootScope', '$cookies', function (AuthenticationService, Notification, $rootScope, $cookies) {
        var vm = this;
        vm.login = login;
        vm.register = register;
        vm.resetpassword = resetpassword;
        vm.logout = logout;
        var hasReset = false;
        function login() {
            vm.dataLoading = true;
            AuthenticationService.Login(vm.username, vm.password, function (response) {
                if (response.success) {
                    Notification.success("登陆成功");
                    showTag(1);
                } else {
                    response.message && Notification.error(response.message);
                }
                vm.dataLoading = false;
            });
        }
        function register() {
            vm.dataLoading = true;
            AuthenticationService.Register(vm.username, vm.password, function (response) {
                if (response.success) {
                    Notification.success("注册成功");
                    showTag(1);
                } else {
                    response.message && Notification.error(response.message);
                }
                vm.dataLoading = false;
            });
        }
        function resetpassword() {
            if (hasReset) {
                Notification.warning("邮件已发送，请查看您的电子邮件以重置密码");
                return;
            }
            vm.dataLoading = true;
            AuthenticationService.Reset(vm.username, function (response) {
                if (response.success) {
                    Notification.success("请查看您的电子邮件以重置密码");
                    hasReset = true;
                } else {
                    response.message && Notification.error(response.message);
                }
                vm.dataLoading = false;
            });
        }
        function logout() {
            var user = $cookies.get('user');
            if (user !== null) {
                AuthenticationService.Logout(function (response) {
                    if (response.success) {
                        Notification.success("注销成功");
                    } else {
                        response.message && Notification.error(response.message);
                    }
                });
            }
            else {
                $rootScope.user.isLogin = false;
            }
        }
    }]);
})();