#pragma once
#include "CoreMinimal.h"
#include "OnTitleStorageFileDownloadedDelegate.generated.h"

class UTitleStorageFile;

UDELEGATE(BlueprintCallable) DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnTitleStorageFileDownloaded, bool, bSuccess, UTitleStorageFile*, File);

