#pragma once
#include "CoreMinimal.h"
#include "OnTitleStorageFileManifestDownloadedDelegate.generated.h"

class UTitleStorageFile;

UDELEGATE(BlueprintCallable) DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnTitleStorageFileManifestDownloaded, bool, bSuccess, const TArray<UTitleStorageFile*>&, FileManifest);

