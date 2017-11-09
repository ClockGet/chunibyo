(function () {
    'use strict';
    // UserService
    angular
        .module('user.service',[])
        .factory('UserService', UserService);

    UserService.$inject = ['$http','$httpParamSerializerJQLike'];
    function UserService($http,$httpParamSerializerJQLike) {
        var service = {};
        service.Login = Login;
        service.Create = Create;
        service.Logout = Logout;
        service.Reset = Reset;
        return service;
        function Login(user) {
            return $http.post('api/users/login', $httpParamSerializerJQLike(user)).then(handleSuccess, handleError('Error login user'));
        }
        function Create(user) {
            return $http.post('/api/users/create', $httpParamSerializerJQLike(user)).then(handleSuccess, handleError('Error creating user'));
        }
        function Logout() {
            return $http.get('/api/users/logout').then(handleSuccess, handleError('Error logout user'));
        }
        function Reset(username) {
            return $http.get('/api/users/reset/' + username).then(handleSuccess, handleError('Error reset user'));
        }
        // private functions

        function handleSuccess(res) {
            return res.data;
        }

        function handleError(error) {
            return function () {
                return { success: false, message: error };
            };
        }
    }

    // AuthenticationService
    angular
        .module('authentication.service',['user.service','encrypt','ngCookies'])
        .factory('AuthenticationService', AuthenticationService);
    AuthenticationService.$inject = ['$http', '$cookies', '$rootScope', '$timeout', 'UserService', 'Md5'];
    function AuthenticationService($http, $cookies, $rootScope, $timeout, UserService, Md5) {
        var service = {};
        service.Login = Login;
        service.Register = Register;
        service.Logout = Logout;
        service.Reset = Reset;
        return service;

        function Login(username, password, callback) {
            $timeout(function () {
                var response = null;
                var pw = Md5.hex_md5(password);
                UserService.Login({ username: username, password:pw })
                    .then(function (result) {
                        if (result !== null && result.success) {
                            angular.extend($rootScope.user, { userName: username, passWord: pw });
                            $rootScope.user.isLogin = true;
                            $rootScope.user.userName = username;
                            var cookieExp = new Date();
                            cookieExp.setDate(cookieExp.getDate() + 7);
                            SetCookie('user', $rootScope.user, { expires: cookieExp });
                            response = { success: true };
                        }else {
                            response = { success: false, message: result.message };
                        }
                        callback(response);
                    });
            },1000);
        }
        function Register(username, password, callback) {
            $timeout(function () {
                var response= null;
                var pw = Md5.hex_md5(password);
                UserService.Create({ userName: username, passWord: pw })
                    .then(function (result) {
                        if (result !== null && result.success) {
                            angular.extend($rootScope.user, { userName: username, passWord: pw });
                            $rootScope.user.isLogin = true;
                            $rootScope.user.userName = username;
                            var cookieExp = new Date();
                            cookieExp.setDate(cookieExp.getDate() + 7);
                            SetCookie('user', $rootScope.user, { expires: cookieExp });
                            response = { success: true };
                        } else {
                            response = { success: false, message: result.message };
                        }
                        callback(response);
                    });
            }, 1000);
        }
        function Logout(callback) {
            $timeout(function () {
                var response= null;
                UserService.Logout()
                    .then(function (result) {
                        if (result !== null && result.success) {
                            $rootScope.user = { isLogin: false };
                            delCookie('user');
                            response = { success: true };
                        } else {
                            response = { success: false, message: result.message };
                        }
                        callback(response);
                    });
            }, 1000);
        }
        function Reset(username, callback) {
            $timeout(function () {
                var response = null;
                UserService.Reset(username)
                    .then(function (result) {
                        if (result !== null && result.success) {
                            response = { success: true };
                        } else {
                            response = { success: false, message: result.message };
                        }
                        callback(response);
                    });
            }, 1000);
        }
        // private functions
        function SetCookie(name, object, options) {
            $cookies.putObject(name, object, options);
        }
        function delCookie(name) {
            $cookies.remove(name);
        }
    }
})();