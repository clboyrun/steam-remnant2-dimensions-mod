#include "EnvQueryGenerator_PathingGrid3D.h"
#include "EnvironmentQuery/Contexts/EnvQueryContext_Querier.h"
#include "EnvironmentQuery/Items/EnvQueryItemType_Point.h"

UEnvQueryGenerator_PathingGrid3D::UEnvQueryGenerator_PathingGrid3D() {
    this->ItemType = UEnvQueryItemType_Point::StaticClass();
    this->GenerateAround = UEnvQueryContext_Querier::StaticClass();
}


