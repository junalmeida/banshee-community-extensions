AC_DEFUN([BCE_SONGKICKGEOLOCATION],
[
	BCE_ARG_DISABLE([SongKickGeoLocation], [yes])

	if test "x$enable_SongKickGeoLocation" = "xyes"; then
		AM_CONDITIONAL(ENABLE_SONGKICKGEOLOCATION, true)
		SHAMROCK_FIND_FSHARP_COMPILER
	else
		AM_CONDITIONAL(ENABLE_SONGKICKGEOLOCATION, false)
	fi
])

