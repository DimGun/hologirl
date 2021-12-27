set -e

MAX_ATTEMPTS=3

if [ $# != 2 ]; then
  echo "Usage:"
  echo "`basename $0` (host_url) ("phrase" | wav-file path)"
  exit 1
fi

HOST=$1
echo $2
if [ "${2##*.}" == "wav" ]; then
  FILENAME=$2
else
  PHRASE=$2
fi

echo "Host name: '$HOST' Filename: '$FILENAME' Phrase: '$PHRASE'"

if [ -z "$PHRASE" ]; then
  request_url=$HOST/voice-request
  echo  "01. Sending request to $request_url"
  curl \
    -X POST $request_url \
    -o 01-voice-request-response.json \
    -H 'Content-Type: multipart/form-data' \
    -F voice=@$FILENAME \
    --silent --show-error
else
  request_url=$HOST/text-request
  echo  "01. Sending request to $request_url"
  curl \
    -X POST $request_url \
    -o 01-voice-request-response.json \
    -H 'Content-Type: multipart/form-data' \
    -F text="$PHRASE" \
    --silent --show-error
fi

echo 'Repsonse:'
jq "." 01-voice-request-response.json
echo "--------------------------"


metadata_url=`jq ".data.metadata" 01-voice-request-response.json | tr -d '"'`
if [ -z $metadata_url ]; then
  echo "Failed to get metadata url" 1>&2
  exit 1
fi
metadata_url="$HOST$metadata_url"

attempt=1
while true; do
  echo "02. Fetching metadata from $metadata_url. Attempt #$attempt"

  curl \
    -X GET "$metadata_url" \
    -o 02-metadata-response.json \
    --silent --show-error
  echo 'Repsonse:'
  jq "." 02-metadata-response.json

  # Check response readiness
  is_voice_ready=`jq ".data.voice_ready" 02-metadata-response.json`
  if [ "x$is_voice_ready" == "xtrue" ]; then
    echo "Voice response is ready"
    break
  else
    echo "Voice response is not ready."
    if [ $attempt -lt $MAX_ATTEMPTS ]; then
      echo "Making a new attempt"
      ((attempt++))
      sleep 1
    else
      echo "Failed to get voice response in $attempt attempts. Giving up."
      exit 1
    fi
  fi
  echo "--------------------------"
done

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

rm 01-voice-request-response.json
rm 02-metadata-response.json
rm 03-voice-response.wav
