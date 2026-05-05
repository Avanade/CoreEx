-- Create table: "test"."table"

CREATE TABLE "test"."table" (
    "table_id" UUID NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "text" VARCHAR(200) NULL,
    "number" INT NULL,
    "amount" DECIMAL(19, 4) NULL,
    "flag" BOOLEAN NULL,
    "date" DATE NULL,
    "time" TIME NULL,
    "json" JSONB NULL,
    "tenant_id" VARCHAR(20) NULL,
    "created_by" VARCHAR(250) NULL,
    "created_on" TIMESTAMPTZ NULL,
    "updated_by" VARCHAR(250) NULL,
    "updated_on" TIMESTAMPTZ NULL,
    "is_deleted" BOOLEAN DEFAULT false NOT NULL
);