@echo off
set e=echo.^&echo
:setup
set curl_get=curl -X GET -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_post=curl -X POST -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_put=curl -X PUT -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
set curl_del=curl -X DELETE -s -w "\n%%{http_code}\n" -H "Authorization: %auth%"
if "%cont%" neq "" (goto %cont%)

:auths
%e% Login with wrong credentials, should fail
%curl_post% http://localhost:10001/login -d "{\"username\":\"gibtsned\", \"password\":\"nope\"}"

%e% Register tester0 account, may work depending on whether you cleared the database before
%curl_post% http://localhost:10001/register -d "{\"username\": \"tester0\", \"password\": \"tester0pw\"}"
rem This call already provides an authorization token but we're going to login again anyway...

%e% Register tester0 account again, should definetly not work
%curl_post% http://localhost:10001/register -d "{\"username\": \"tester0\", \"password\": \"tester0pw\"}"

%e% Login with correct credentials, should work
%curl_post% http://localhost:10001/login -d "{\"username\": \"tester0\", \"password\": \"tester0pw\"}"


:demo
%e% Create demo data (package and cards with fixed IDs)
%curl_post% http://localhost:10001/demo

%e% Login with correct credentials, should work
%curl_post% -D - http://localhost:10001/login -d "{\"username\": \"tester2\", \"password\": \"tester2pw\"}"

%e% Next up: profile stuff
set /p auth=Copy Authorization value here: 
set cont=postauth
goto setup


:postauth
%e% Get own profile
%curl_get% http://localhost:10001/profile

%e% Update own profile
%curl_post% http://localhost:10001/profile -d "{\"StatusText\":\"neuer Status\", \"EmoticonText\": \":o\"}"

%e% Update deck
%curl_post% http://localhost:10001/deck -d "[ \"00000001-0000-0000-0000-000000000011\", \"00000001-0000-0000-0000-000000000006\", \"00000001-0000-0000-0000-000000000010\", \"00000001-0000-0000-0000-000000000002\" ]"

%e% Get profile of tester1
%curl_get% http://localhost:10001/profile/tester1

%e% Get profile of tester2 (own, for checking)
%curl_get% http://localhost:10001/profile/tester2

%e% Get own stack
%curl_get% http://localhost:10001/stack

%e% Get own deck
%curl_get% http://localhost:10001/deck

%e% Get own stack (alternate way)
%curl_get% http://localhost:10001/profile/tester2/stack

%e% Get own deck (alternate way)
%curl_get% http://localhost:10001/profile/tester2/deck


%e% Next up: listings
pause>nul


%e% Mark card as tradable
%curl_post% http://localhost:10001/store/cards/new -d "{\"CardId\": \"00000001-0000-0000-0000-000000000015\", \"Requirements\": [ { \"RequirementType\": \"IsMonsterCard\" }, { \"RequirementType\": \"MinimumDamage\", \"MinimumDamage\": 10 } ]}"

%e% Show tradable cards, should contain one entry
%curl_get% http://localhost:10001/store/cards

%e% Show packages
%curl_get% http://localhost:10001/store/packages

%e% Show highest ELO scoreboard
%curl_get% http://localhost:10001/scoreboards/highestelo

%e% Show most wins scoreboard
%curl_get% http://localhost:10001/scoreboards/mostwins

%e% Show least losses scoreboard
%curl_get% http://localhost:10001/scoreboards/leastlosses

%e% Show best win/loss ratio
%curl_get% http://localhost:10001/scoreboards/bestwlratio


%e% Next up: packages and trading
pause>nul


:pack
%e% Create new package
%curl_post% http://localhost:10001/store/packages -d "{\"Price\":12,\"Cards\":[{\"CardType\":\"Goblin\",\"Damage\":11},{\"CardType\":\"Dragon\",\"Damage\":70},{\"CardType\":\"WaterSpell\",\"Damage\":22},{\"CardType\":\"Ork\",\"Damage\":40},{\"CardType\":\"NormalSpell\",\"Damage\":28}]}"

%e% Buy specific package (00000004-0000-0000-0000-000000000000)
%curl_post% http://localhost:10001/store/packages/buy/00000004-0000-0000-0000-000000000000

%e% Buy invalid package
%curl_post% http://localhost:10001/store/packages/buy/00000000-0000-0000-0000-000000000000

%e% Buy random package
%curl_post% http://localhost:10001/store/packages/buy/random/package

%e% Buy random cards
%curl_post% http://localhost:10001/store/packages/buy/random/cards

%e% Buy random package with too little money
%curl_post% http://localhost:10001/store/packages/buy/random/package

%e% Buy random cards with too little money
%curl_post% http://localhost:10001/store/packages/buy/random/cards

%e% Confirm by checking profile
%curl_get% http://localhost:10001/profile

%e% Get eligible trade deals, should return above deal
%curl_get% http://localhost:10001/store/cards/eligible -d "\"00000001-0000-0000-0000-000000000014\""

%e% Trade cards
%curl_post% http://localhost:10001/store/cards/trade -d "{ \"OwnCard\": \"00000001-0000-0000-0000-000000000014\", \"OtherCard\": \"00000001-0000-0000-0000-000000000015\" }"


%e% Next up: battling 1 (cancelled)
pause>nul

:battle1
%e% Join battle with same player (1/2)
start /b %curl_post% http://localhost:10001/battle >nul

%e% Join battle with same player (2/2)
%curl_post% http://localhost:10001/battle

%e% Only one player's output is shown to avoid messiness
%e% Next up: battling 2 (other outcome)
pause>nul


%e% Join battle with first player
start /b %curl_post% http://localhost:10001/battle >nul

%e% Login with other account, should work
%curl_post% -D - http://localhost:10001/login -d "{\"username\": \"tester1\", \"password\": \"tester1pw\"}"

set /p auth=Copy Authorization value here: 
set cont=battle2
goto setup


:battle2
%e% Join battle with second player
%curl_post% http://localhost:10001/battle

%e% Get own profile
%curl_get% http://localhost:10001/profile

%e% Next up: finishing
pause>nul

set rnd=%random%
%e% Create new test user
%curl_post% -D - http://localhost:10001/register -d "{\"username\": \"u%rnd%\", \"password\": \"pw\"}"
set /p auth=Copy Authorization value here: 
set cont=end
goto setup


:end
%e% Logout
%curl_post% http://localhost:10001/logout

%e% Finished running tests.
pause>nul
