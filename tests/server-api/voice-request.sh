set -e

HOST=https://ht.studsib.ru

voice_request_url=$HOST/voice-request
echo  "01. Sending request to $voice_request_url"
curl \
  -X POST $voice_request_url \
  -o 01-voice-request-response.json \
  -H 'Content-Type: multipart/form-data' \
  -F voice=@./test-whats-your-name.wav \
  --silent

echo 'Repsonse:'
jq "." 01-voice-request-response.json
echo "--------------------------"

metadata_url=`jq ".data.metadata" 01-voice-request-response.json | tr -d '"'`
metadata_url="$HOST$metadata_url"

echo "02. Fetching metadata from $metadata_url"
curl \
  -X GET "$metadata_url" \
  -o 02-metadata-response.json \
  --silent
echo 'Repsonse:'
jq "." 02-metadata-response.json
echo "--------------------------"

voice_url=`jq ".data.voice" 01-voice-request-response.json | tr -d '"'`
voice_url="$HOST$voice_url"
echo "03. Fetching voice from $voice_url"
curl \
  -X GET $voice_url \
  -o 03-voice-response.wav
echo "--------------------------"

echo -n "Playing response..."
afplay 03-voice-response.wav
echo "OK"
