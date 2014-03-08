"use strict";

var services = angular.module('services', []);

services.factory("ChatService", ['$q', '$rootScope', function($q, $rootScope) {
	var service = {};

	// Ideally I'd access a reference on this service, rather than a property
	// of the root scope, but I've had no success binding to that so far...
	$rootScope.messages = [];

	// Setup WebSocket callbacks
	service.WebSocket = new WebSocket("ws://localhost:81");
	
	service.WebSocket.onopen = function() {
		console.log("socket opened");
	};

	service.WebSocket.onmessage = function(message) {
		var messageJson = JSON.parse(message.data);
		switch (messageJson.Type) {
			case "snapshot":
				// Snapshot of messages received on connect
				$rootScope.$apply(function () {
					$rootScope.messages.length = 0;
					messageJson.Msgs.forEach(function (message, i, array) {
						$rootScope.messages.push(message);
					});
				})
				break;

			case "msg":
				// New message received
				console.log(">> " + service.Id);
				$rootScope.$apply(function () {
					$rootScope.messages.push(messageJson);
				})
				break;
		}
		console.log(JSON.parse(message.data));
	};

	service.SendMessage = function (message) {
		service.WebSocket.send(message);
	};

	return service;
}]);
