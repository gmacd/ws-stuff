"use strict";

var controllers = angular.module("controllers", []);

controllers.controller("ChatController", ["$scope", "ChatService", function ($scope, ChatService) {

	// Send message when enter pressed in message textarea
	$scope.submitMessage = function (keyCode) {
		if (keyCode === 13) {
			// Enter pressed
			ChatService.sendMessage(JSON.stringify({"Type": "newMsg", "Message": $scope.form.newMessage}));
			$scope.form.newMessage = "";
		}
	}
}]);
