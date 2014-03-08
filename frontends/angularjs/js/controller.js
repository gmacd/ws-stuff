"use strict";

var controllers = angular.module("controllers", []);

controllers.controller("ChatController", ["$scope", "ChatService", function ($scope, ChatService) {

	$scope.SendMessage = function () {
		ChatService.SendMessage(JSON.stringify({"Type": "newMsg", "Message": $scope.form.newMessage}));
		$scope.form.newMessage = "";
	}
}]);
