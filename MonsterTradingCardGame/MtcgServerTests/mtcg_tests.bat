@echo off
set curl_get=curl -X GET -s -w "\n%%{http_code}\n"
set curl_post=curl -X POST -s -w "\n%%{http_code}\n"
set curl_put=curl -X PUT -s -w "\n%%{http_code}\n"
set curl_del=curl -X DELETE -s -w "\n%%{http_code}\n"

echo Login with wrong credentials, should fail
%curl_post% http://localhost:10001/login -d "{\"username\":\"gibtsned\", \"password\":\"nope\"}"

echo Login with correct credentials, should work
%curl_post% http://localhost:10001/login -d "{\"username\": \"tester1\",\"password\": \"tester1pw\"}"
