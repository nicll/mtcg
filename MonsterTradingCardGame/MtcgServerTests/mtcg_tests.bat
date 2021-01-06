@echo off
:setup
set curl_get=curl -X GET -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_post=curl -X POST -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_put=curl -X PUT -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_del=curl -X DELETE -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
if "%cont%" neq "" (goto %cont%)

:auths
echo Login with wrong credentials, should fail
%curl_post% http://localhost:10001/login -d "{\"username\":\"gibtsned\", \"password\":\"nope\"}"

echo Register tester0 account, may work depending on whether you cleared the database before
%curl_post% http://localhost:10001/register -d "{\"username\": \"tester0\", \"password\": \"tester0pw\"}"
rem This call already provides an authorization token but we're going to login again anyway...

echo Login with correct credentials, should work
%curl_post% http://localhost:10001/login -d "{\"username\": \"tester0\", \"password\": \"tester0pw\"}"


:demo
echo Create demo data (package and cards with fixed IDs)
%curl_post% http://localhost:10001/demo

echo Login with correct credentials, should work
%curl_post% -D - http://localhost:10001/login -d "{\"username\": \"tester2\", \"password\": \"tester2pw\"}"

set /p auth=Copy Authorization value here: 
set cont=postauth
goto setup


:postauth
echo Get own profile
%curl_get% http://localhost:10001/profile

echo Update own profile
%curl_post% http://localhost:10001/profile -d "{\"StatusText\":\"neuer Status\", \"EmoticonText\": \":o\"}"

echo Update deck
%curl_post% http://localhost:10001/deck -d "[ \"00000001-0000-0000-0000-000000000011\", \"00000001-0000-0000-0000-000000000006\", \"00000001-0000-0000-0000-000000000010\", \"00000001-0000-0000-0000-000000000002\" ]"

echo Get profile of tester1
%curl_get% http://localhost:10001/profile/tester1

echo Get profile of tester2 (own, for checking)
%curl_get% http://localhost:10001/profile/tester2

echo Get own stack
%curl_get% http://localhost:10001/stack

echo Get own deck
%curl_get% http://localhost:10001/deck

echo Get own stack (alternate way)
%curl_get% http://localhost:10001/profile/tester2/stack

echo Get own deck (alternate way)
%curl_get% http://localhost:10001/profile/tester2/deck

echo Show tradable cards
%curl_get% http://localhost:10001/store/cards

echo Mark card as tradable
%curl_post% http://localhost:10001/store/cards/new -d "{\"CardId\": \"00000001-0000-0000-0000-000000000015\", \"Requirements\": [ { \"RequirementType\": \"IsMonsterCard\" }, { \"RequirementType\": \"MinimumDamage\": 10 } ]}"

echo Show packages
%curl_get% http://localhost:10001/store/packages

echo Show highest ELO scoreboard
%curl_get% http://localhost:10001/scoreboards/highestelo

echo Show most wins scoreboard
%curl_get% http://localhost:10001/scoreboards/mostwins

echo Show least losses scoreboard
%curl_get% http://localhost:10001/scoreboards/leastlosses

echo Show least losses scoreboard
%curl_get% http://localhost:10001/scoreboards/bestwlratio


:pack
echo Buy specific package (00000004-0000-0000-0000-000000000000)
%curl_post% http://localhost:10001/store/packages/buy/00000004-0000-0000-0000-000000000000

echo Buy random package
%curl_post% http://localhost:10001/store/packages/buy/random/package

echo Buy random cards
%curl_post% http://localhost:10001/store/packages/buy/random/cards

rem TODO: POST /BATTLE, POST /store/cards/trade, GET /store/cards/eligible

set rnd=%random%
echo Create new test user
%curl_post% -D - http://localhost:10001/login -d "{\"username\": \"u%rnd%\", \"password\": \"pw\"}"
set /p auth=Copy Authorization value here: 
set cont=pack
goto setup


:end
echo Logout
%curl_post% http://localhost:10001/logout

pause
