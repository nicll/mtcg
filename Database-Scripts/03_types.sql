-- Type: card_req_types

-- DROP TYPE public.card_req_types;

CREATE TYPE public.card_req_types AS ENUM
    ('element_type', 'is_monster_card', 'is_spell_card', 'minimum_damage');

ALTER TYPE public.card_req_types
    OWNER TO postgres;

GRANT USAGE ON TYPE public.card_req_types TO PUBLIC;

GRANT USAGE ON TYPE public.card_req_types TO mtcg_user;

GRANT USAGE ON TYPE public.card_req_types TO postgres;



-- Type: element_type

-- DROP TYPE public.element_type;

CREATE TYPE public.element_type AS ENUM
    ('normal', 'water', 'fire');

ALTER TYPE public.element_type
    OWNER TO postgres;

GRANT USAGE ON TYPE public.element_type TO PUBLIC;

GRANT USAGE ON TYPE public.element_type TO mtcg_user;

GRANT USAGE ON TYPE public.element_type TO postgres;



-- Type: monster_type

-- DROP TYPE public.monster_type;

CREATE TYPE public.monster_type AS ENUM
    ('dragon', 'fireelf', 'goblin', 'knight', 'kraken', 'ork', 'wizard', 'spell');

ALTER TYPE public.monster_type
    OWNER TO postgres;

GRANT USAGE ON TYPE public.monster_type TO PUBLIC;

GRANT USAGE ON TYPE public.monster_type TO mtcg_user;

GRANT USAGE ON TYPE public.monster_type TO postgres;



-- Type: card_req

-- DROP TYPE public.card_req;

CREATE TYPE public.card_req AS
(
	req_type card_req_types,
	req_value integer
);

ALTER TYPE public.card_req
    OWNER TO postgres;

GRANT USAGE ON TYPE public.card_req TO PUBLIC;

GRANT USAGE ON TYPE public.card_req TO mtcg_user;

GRANT USAGE ON TYPE public.card_req TO postgres;

