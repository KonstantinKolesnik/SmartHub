﻿define(['application/sections/list'],
	function (sections) {
	    return {
	        start: function () {
	            sections.api.reload('loadCommonSections', 'Приложения');
	        }
	    };
	});