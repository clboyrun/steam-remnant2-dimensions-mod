#include "AssetCache.h"

UAssetCache::UAssetCache() {
    this->PreloadMethod = EAssetCachePreloadMethod::OnGameInstanceInit;
}

void UAssetCache::PreloadAssets() {
}

bool UAssetCache::IsAssetCachedPreloaded() {
    return false;
}

UAssetCache* UAssetCache::GetInstance() {
    return NULL;
}


