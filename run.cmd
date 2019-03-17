@echo off

pushd %~dp0

set FXS_PATH=%1
call %1\run.cmd +exec server.cfg +exec server_private.cfg

popd