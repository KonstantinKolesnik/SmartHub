﻿
define(
    ['marionette', 'backbone', 'jquery', 'signalR'],
    function (marionette, backbone, $, signalR) {
	    var api = {
		    parseParameters: function (queryString) {
			    var result = [];

			    if (queryString !== null && queryString !== undefined) {
				    var params = (queryString + '').split('/');

				    for (var i = 0; i < params.length; i++) {
					    var decodedValue = decodeURIComponent(params[i]);
					    result.push(decodedValue);
				    }
			    }

			    return result;
		    },
		    loadRoute: function (route, args) {
			    if (route) {
				    require([route], function (obj) {
					    obj.start.apply(obj, args);

					    if (args && args.length) {
						    var encoded = [];

						    for (var i = 0; i < args.length; i++)
							    encoded.push(encodeURIComponent(args[i]));

						    route += '?' + encoded.join('/');
					    }

					    backbone.history.navigate(route);
				    });
			    }
		    }
	    };

	    var app = new marionette.Application();

	    app.addRegions({ regionContent: "#region-page-content" });
	    app.setContentView = function (view) {
		    app.regionContent.show(view);
	    };

	    app.addTile = function (typeFullName, parameters) {
	        $.post(
                '/api/webui/tiles/add',
                {
                    typeFullName: typeFullName,
                    parameters: JSON.stringify(parameters)
                })
			    .done(function () {
				    app.navigate('tiles');
			    });
	    };

	    app.navigate = function (route) {
		    var args = Array.prototype.slice.call(arguments, 1);
		    api.loadRoute.call(this, route, args);
	    };
	    app.loadPath = function (route, args) {
		    api.loadRoute.call(this, route, args);
	    };

	    app.router = new marionette.AppRouter({
		    appRoutes: { '*path': 'loadPage' },
		    controller: {
			    loadPage: function (route, queryString) {
				    var args = api.parseParameters(queryString);
				    api.loadRoute.call(this, route, args);
			    }
		    }
	    });

	    app.wsClient = null;
	    app.wsServer = null;

	    app.initSignalR = function (onComplete) {
	        var hostName = document.location.hostname;

	        $.getScript('http://' + hostName + ':55556/signalr/hubs', function () {
	            //Set the hubs URL for the connection
	            $.connection.hub.url = "http://" + hostName + ":55556/signalr";

	            // Create a function that the hub can call to broadcast messages.
	            $.connection.myHub.client.onServerMessage = app.onServerMessage;

	            // Start the connection.
	            $.connection.hub.start().done(function () {
	                app.wsClient = $.connection.myHub.client;
	                app.wsServer = $.connection.myHub.server;

	                onComplete();
	            });
	        });
	    }
	    app.onServerMessage = function (name, message) {
	        debugger;
	    }
	    app.sendSignalRMessage = function (name, message) {
	        app.wsServer.onClientMessage(name, message);
	    }

	    app.on('start', function () {
	        app.initSignalR(function () { });

	        if (backbone.history) {
			    backbone.history.start();

			    if (Backbone.history.fragment === '')
				    app.navigate('tiles');
		    }
	    });

	    return app;
    });