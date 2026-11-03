#pragma once
#include "CoreMinimal.h"
#include "ETitleStorageLocalFileState.h"
#include "TitleStorageFileHeader.generated.h"

USTRUCT(BlueprintType)
struct GUNFIREONLINEMODULE_API FTitleStorageFileHeader {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FString Filename;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    int32 FileSize;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    int32 DownloadSize;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    ETitleStorageLocalFileState LocalFileState;
    
    FTitleStorageFileHeader();
};

