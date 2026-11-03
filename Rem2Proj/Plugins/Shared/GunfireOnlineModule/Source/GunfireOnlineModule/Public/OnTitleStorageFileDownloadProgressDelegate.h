#pragma once
#include "CoreMinimal.h"
#include "OnTitleStorageFileDownloadProgressDelegate.generated.h"

class UTitleStorageFile;

UDELEGATE() DECLARE_DYNAMIC_MULTICAST_DELEGATE_ThreeParams(FOnTitleStorageFileDownloadProgress, UTitleStorageFile*, File, int64, BytesReceived, float, PercentComplete);

