#include "EnvQueryGenerator_AITacticalPoint.h"
#include "EnvironmentQuery/Contexts/EnvQueryContext_Querier.h"
#include "EnvQueryItemType_AITacticalPoint.h"

UEnvQueryGenerator_AITacticalPoint::UEnvQueryGenerator_AITacticalPoint() {
    this->ItemType = UEnvQueryItemType_AITacticalPoint::StaticClass();
    this->GenerateAround = UEnvQueryContext_Querier::StaticClass();
}


